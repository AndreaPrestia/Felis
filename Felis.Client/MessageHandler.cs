using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Felis.Core;
using Felis.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Felis.Client;

public sealed class MessageHandler : IAsyncDisposable
{
    private readonly HubConnection? _hubConnection;
    private readonly ILogger<MessageHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Service _currentService;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;
    private readonly HttpClient _httpClient;
    private readonly RetryPolicy? _retryPolicy;
    private readonly Dictionary<Topic, Type>? _consumers;

    public MessageHandler(HubConnection? hubConnection, ILogger<MessageHandler> logger,
        IOptionsMonitor<FelisConfiguration> configuration, IServiceProvider serviceProvider, IMemoryCache cache, HttpClient httpClient)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (string.IsNullOrWhiteSpace(configuration.CurrentValue.Router?.Endpoint))
        {
            throw new ArgumentNullException($"No Router:Endpoint configuration provided");
        }

        if (configuration.CurrentValue.Cache == null)
        {
            throw new ArgumentNullException($"No Cache configuration provided");
        }

        _retryPolicy = configuration.CurrentValue.RetryPolicy ?? throw new ArgumentNullException($"No RetryPolicy configuration provided");
        
        _cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(configuration.CurrentValue.Cache.SlidingExpiration))
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(configuration.CurrentValue.Cache.AbsoluteExpiration))
            .SetSize(configuration.CurrentValue.Cache.MaxSizeBytes)
            .SetPriority(CacheItemPriority.High);

        _currentService = configuration.CurrentValue.Service ?? throw new ArgumentNullException(nameof(configuration.CurrentValue.Service));
        
        _consumers = GetTypesForTopics();
    }

    public async Task Publish<T>(T payload, string? topic, CancellationToken cancellationToken = default)
        where T : class
    {
        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        try
        {
            await CheckHubConnectionStateAndStartIt(cancellationToken);

            //TODO add an authorization token as parameter

            var type = payload.GetType().FullName;

            var json = JsonSerializer.Serialize(payload);

            var responseMessage = await _httpClient.PostAsJsonAsync("/dispatch",
                new Message(new Header(new Topic(topic ?? type), new List<Service>()), new Content(json)),
                cancellationToken: cancellationToken);

            responseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    public async Task Publish<T>(T payload, string? topic, List<Service>? services,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (services == null || !services.Any())
        {
            throw new ArgumentNullException(nameof(services));
        }

        try
        {
            await CheckHubConnectionStateAndStartIt(cancellationToken);

            var connectedServices = await GetConnectedServices(cancellationToken);

            if (connectedServices == null || !connectedServices.Any())
            {
                _logger.LogWarning("No connected services to dispatch. The message won't be published");
                return;
            }

            if (!connectedServices.Select(x => x).Intersect(services).Any())
            {
                _logger.LogWarning(
                    "No connected services available in the list provided to dispatch. The message won't be published");
                return;
            }

            //TODO add an authorization token as parameter
            var json = JsonSerializer.Serialize(payload);

            var responseMessage = await _httpClient.PostAsJsonAsync("/dispatch",
                new Message(new Header(new Topic(topic), services), new Content(json)),
                cancellationToken: cancellationToken);

            responseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    internal async Task Subscribe(CancellationToken cancellationToken = default)
    {
        if (_hubConnection == null)
        {
            throw new ArgumentNullException($"Connection to Felis router not correctly initialized");
        }

        _currentService.Topics = GetTopicsFromCurrentInstance();

        foreach (var topic in _currentService.Topics)
        {
            if (string.IsNullOrWhiteSpace(topic.Value))
            {
                continue;
            }
            
            _hubConnection.On<Message?>(topic.Value, async (messageIncoming) =>
            {
                try
                {
                    if (messageIncoming == null)
                    {
                        throw new ArgumentNullException(nameof(messageIncoming));
                    }

                    if (messageIncoming.Header?.Topic == null ||
                        string.IsNullOrWhiteSpace(messageIncoming.Header?.Topic?.Value))
                    {
                        throw new ArgumentNullException($"No Topic provided in Header");
                    }

                    if (string.IsNullOrWhiteSpace(_hubConnection?.ConnectionId))
                    {
                        throw new ArgumentNullException(nameof(_hubConnection.ConnectionId));
                    }

                    var consumerSearchResult = GetConsumer(topic);

                    if (consumerSearchResult.Consumer == null!)
                    {
                        _logger.LogInformation(
                            $"Consumer not found for topic {messageIncoming.Header?.Topic?.Value}");
                        return;
                    }

                    var consumer = consumerSearchResult.Consumer;

                    var entityType = consumerSearchResult.MessageType;

                    var processMethod = GetProcessMethod(consumerSearchResult.ConsumerType, entityType);

                    if (processMethod == null)
                    {
                        throw new EntryPointNotFoundException(
                            $"No implementation of method {entityType?.Name} Process({entityType?.Name} entity)");
                    }

                    var entity = Deserialize(messageIncoming.Content?.Json, entityType);

                    if (entity == null)
                    {
                        throw new ArgumentNullException(nameof(entity));
                    }

                    var responseMessage = await _httpClient.PostAsJsonAsync($"/consume",
                        new ConsumedMessage(messageIncoming,
                            _currentService),
                        cancellationToken: cancellationToken);

                    responseMessage.EnsureSuccessStatusCode();

                    processMethod.Invoke(consumer, new[] { entity });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    await SendError(messageIncoming, ex, cancellationToken);
                }
            });
        }


        await CheckHubConnectionStateAndStartIt(cancellationToken);
    }

    private async Task CheckHubConnectionStateAndStartIt(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_hubConnection == null)
            {
                throw new ArgumentNullException(nameof(_hubConnection));
            }

            if (_hubConnection?.State == HubConnectionState.Disconnected)
            {
                await _hubConnection?.StartAsync(cancellationToken)!;
            }

            await _hubConnection?.InvokeAsync("SetConnectionId", _currentService, cancellationToken)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection?.InvokeAsync("RemoveConnectionIds", _currentService)!;
            await _hubConnection.DisposeAsync();
        }
    }

    #region PrivateMethods

    private ConsumerSearchResult GetConsumer(Topic? topic)
    {
        if (_consumers == null)
        {
            throw new ApplicationException("No consumers registered.");
        }

        if (topic == null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var typeForTopic = _consumers.FirstOrDefault(x => x.Key.Value == topic.Value);

        var scope = _serviceProvider.CreateScope();
        var provider = scope.ServiceProvider;

        var service = provider.GetService(typeForTopic.Value);

        if (service == null)
        {
            throw new ApplicationException($"No consumer registered for topic {topic.Value}");
        }

        var processParameterInfo = typeForTopic.Value.GetMethod("Process")?.GetParameters().FirstOrDefault();

        if (processParameterInfo == null)
        {
            throw new InvalidOperationException($"Not found parameter of Consumer.Process for topic {topic}");
        }

        var parameterType = processParameterInfo.ParameterType;

        return new ConsumerSearchResult()
        {
            Consumer = service,
            ConsumerType = typeForTopic.Value,
            MessageType = parameterType
        };
    }

    private MethodInfo? GetProcessMethod(Type? consumerType, Type? entityType)
    {
        if (consumerType == null)
        {
            throw new ArgumentNullException(nameof(consumerType));
        }

        if (entityType == null)
        {
            throw new ArgumentNullException(nameof(entityType));
        }

        return consumerType.GetMethods().Where(t => t.Name.Equals("Process")
                                                    && t.GetParameters().Length == 1 &&
                                                    t.GetParameters().FirstOrDefault()!.ParameterType
                                                        .Name.Equals(entityType.Name)
            ).Select(x => x)
            .FirstOrDefault();
    }

    private object? Deserialize(string? content, Type? type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return string.IsNullOrWhiteSpace(content)
            ? null
            : JsonSerializer.Deserialize(content, type, new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            });
    }

    private async Task SendError(Message? message, Exception exception, CancellationToken cancellationToken = default)
    {
        try
        {
            var responseMessage = await _httpClient.PostAsJsonAsync("/error",
                new ErrorMessage(message,
                    _currentService, exception, _retryPolicy),
                cancellationToken: cancellationToken);

            responseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    private async Task<List<Service>?> GetConnectedServices(CancellationToken cancellationToken = default)
    {
        try
        {
            var responseMessage = await _httpClient
                .GetAsync("/services", cancellationToken)
                .ConfigureAwait(false);

            responseMessage.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<List<Service>>(await responseMessage.Content
                .ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<Service>();
        }
    }

    private void SetInCache(string? topic, ConsumerSearchResult value)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentNullException(nameof(topic));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        _cache.Remove(topic);
        _cache.Set(topic, value, _cacheEntryOptions);
    }

    private ConsumerSearchResult? GetFromCache(string? topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var found = _cache.TryGetValue(topic, out ConsumerSearchResult? result);

        return !found ? default : result;
    }

    private List<Topic> GetTopicsFromCurrentInstance()
    {
        var topics = AppDomain.CurrentDomain.GetAssemblies()
            .First(x => x.GetName().Name == AppDomain.CurrentDomain.FriendlyName).GetTypes().Where(t =>
                t.BaseType?.FullName != null
                && t.BaseType.FullName.Contains("Felis.Client.Consume") &&
                t is { IsInterface: false, IsAbstract: false }).SelectMany(t =>
                t.GetCustomAttributes<TopicAttribute>().Select(x => new Topic(x.Value))).ToList();

        if (topics == null)
        {
            throw new InvalidOperationException("Not found implementation of Consumer for any topic");
        }

        return topics;
    }
    
    private Dictionary<Topic, Type> GetTypesForTopics()
    {
        var topicTypes = AppDomain.CurrentDomain.GetAssemblies()
            .First(x => x.GetName().Name == AppDomain.CurrentDomain.FriendlyName).GetTypes().Where(t =>
                t.BaseType?.FullName != null
                && t.BaseType.FullName.Contains("Felis.Client.Consume") &&
                t is { IsInterface: false, IsAbstract: false }).SelectMany(t =>
                t.GetCustomAttributes<TopicAttribute>()
                    .Select(x => new KeyValuePair<Topic, Type>(new Topic(x.Value), t)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        if (topicTypes == null)
        {
            throw new InvalidOperationException("Not found implementation of Consumer for any topic");
        }

        return topicTypes;
    }

    private class ConsumerSearchResult
    {
        public object? Consumer { get; init; }
        public Type? ConsumerType { get; init; }
        public Type? MessageType { get; init; }
    }

    #endregion
}