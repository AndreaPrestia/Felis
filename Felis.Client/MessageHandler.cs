using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Felis.Core;
using Felis.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Felis.Client;

public sealed class MessageHandler : IAsyncDisposable
{
    private readonly HubConnection? _hubConnection;
    private readonly ILogger<MessageHandler> _logger;
    private readonly FelisConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _topic = "NewDispatchedMethod";
    private readonly Service _currentService;

    public MessageHandler(HubConnection? hubConnection, ILogger<MessageHandler> logger,
        FelisConfiguration configuration, IServiceProvider serviceProvider)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(_configuration.Router?.Endpoint))
        {
            throw new ArgumentNullException($"No Router:Endpoint configuration provided");
        }

        _currentService = _configuration.Service ?? throw new ArgumentNullException(nameof(_configuration.Service));
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

            using var client = new HttpClient();
            var responseMessage = await client.PostAsJsonAsync($"{_configuration.Router?.Endpoint}/dispatch",
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

            using var client = new HttpClient();
            var responseMessage = await client.PostAsJsonAsync($"{_configuration.Router?.Endpoint}/dispatch",
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

        _hubConnection.On<Message?>(_topic, async (messageIncoming) =>
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
               
                var consumerSearchResult = GetConsumer(messageIncoming.Header?.Topic?.Value);

                if (consumerSearchResult.Consumer == null!)
                {
                    _logger.LogInformation(
                        $"Consumer not found for topic {messageIncoming.Header?.Topic?.Value}");
                    return;
                }

                var consumer = consumerSearchResult.Consumer;

                var entityType = consumerSearchResult.MessageType;
                
                var processMethod = GetProcessMethod(consumer, entityType);

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

                using var client = new HttpClient();
                var responseMessage = await client.PostAsJsonAsync($"{_configuration.Router?.Endpoint}/consume",
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

    private ConsumerSearchResult GetConsumer(string? topic)
    {
        var constructed = AppDomain.CurrentDomain.GetAssemblies()
            .First(x => x.GetName().Name == AppDomain.CurrentDomain.FriendlyName).GetTypes().FirstOrDefault(t =>
                t.BaseType?.FullName != null
                && t.BaseType.FullName.Contains("Felis.Client.Consume") && t is { IsInterface: false, IsAbstract: false } 
                && t.GetCustomAttributes<TopicAttribute>().Count(x => string.Equals(topic, x.Value, StringComparison.InvariantCultureIgnoreCase)) == 1 
                && t.GetMethods().Any(x => x.Name == "Process"
                                           && x.GetParameters().Count() ==
                                           1));

        if (constructed == null)
        {
            throw new InvalidOperationException($"Not found implementation of Consumer for topic {topic}");
        }


        var firstConstructor = constructed.GetConstructors().FirstOrDefault();

        var parameters = new List<object>();

        if (firstConstructor == null)
        {
            throw new NotImplementedException($"Constructor not implemented in {constructed.Name}");
        }

        foreach (var param in firstConstructor.GetParameters())
        {
            using var serviceScope = _serviceProvider.CreateScope();
            var provider = serviceScope.ServiceProvider;

            var service = provider.GetService(param.ParameterType);

            parameters.Add(service!);
        }

        var processParameterInfo = constructed.GetMethod("Process")?.GetParameters().FirstOrDefault();

        if (processParameterInfo == null)
        {
            throw new InvalidOperationException($"Not found parameter of Consumer.Process for topic {topic}");
        }

        var parameterType = processParameterInfo.ParameterType;

        var instance = Activator.CreateInstance(constructed, parameters.ToArray())!;

        if (instance == null!)
        {
            throw new ApplicationException($"Cannot create an instance of {constructed.Name}");
        }

        return new ConsumerSearchResult()
        {
            MessageType = parameterType,
            Consumer = instance
        };
    }

    private MethodInfo? GetProcessMethod(object instance, Type? entityType)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (entityType == null)
        {
            throw new ArgumentNullException(nameof(entityType));
        }

        return instance.GetType().GetMethods().Where(t => t.Name.Equals("Process")
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
            using var client = new HttpClient();
            var responseMessage = await client.PostAsJsonAsync($"{_configuration.Router?.Endpoint}/error",
                new ErrorMessage(message,
                    _currentService, exception, _configuration.RetryPolicy),
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
            using var client = new HttpClient();
            var responseMessage = await client
                .GetAsync($"{_configuration.Router?.Endpoint}/services", cancellationToken)
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

    private class ConsumerSearchResult
    {
        public object? Consumer { get; init; }
        public Type? MessageType { get; init; }
    }

    #endregion
}