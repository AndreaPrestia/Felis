using System.Net.Http.Json;
using System.Text.Json;
using Felis.Core;
using Felis.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Felis.Client;

public sealed class MessageHandler : IAsyncDisposable
{
    private readonly HubConnection? _hubConnection;
    private readonly ILogger<MessageHandler> _logger;
    private readonly Service _currentService;
    private readonly HttpClient _httpClient;
    private readonly RetryPolicy? _retryPolicy;
    private readonly ConsumerResolver _consumerResolver;

    public MessageHandler(HubConnection? hubConnection, ILogger<MessageHandler> logger,
        IOptionsMonitor<FelisConfiguration> configuration, HttpClient httpClient, ConsumerResolver consumerResolver)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _consumerResolver = consumerResolver ?? throw new ArgumentNullException(nameof(consumerResolver));

        _retryPolicy = configuration.CurrentValue.RetryPolicy ?? throw new ArgumentNullException($"No RetryPolicy configuration provided");

        if (configuration.CurrentValue.Service == null)
        {
            throw new ArgumentNullException(nameof(configuration.CurrentValue.Service));
        }
        
        _currentService = new Service(configuration.CurrentValue.Service.Name, configuration.CurrentValue.Service.Host, configuration.CurrentValue.Service.IsPublic, new List<Topic>());
    }

    public async Task PublishAsync<T>(T payload, string? topic, CancellationToken cancellationToken = default)
        where T : class
    {
        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        try
        {
            await CheckHubConnectionStateAndStartIt(cancellationToken).ConfigureAwait(false);

            //TODO add an authorization token as parameter

            var type = payload.GetType().FullName;

            var json = JsonSerializer.Serialize(payload);

            var responseMessage = await _httpClient.PostAsJsonAsync("/dispatch",
                new Message(new Header(new Topic(topic ?? type), new List<Service>()), new Content(json)),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            responseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    public async Task SubscribeAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection == null)
        {
            throw new ArgumentNullException($"Connection to Felis router not correctly initialized");
        }

        var topicsTypes = _consumerResolver.GetTypesForTopics();

        _currentService.Topics = topicsTypes.Select(x => x.Key).ToList();

        foreach (var topicType in topicsTypes)
        {
            if (string.IsNullOrWhiteSpace(topicType.Key.Value))
            {
                continue;
            }
            
            _hubConnection.On<Message?>(topicType.Key.Value, async (messageIncoming) =>
            {
                try
                {
                    if (messageIncoming == null)
                    {
                        _logger.LogWarning("No message incoming.");
                        return;
                    }

                    if (messageIncoming.Header?.Topic == null ||
                        string.IsNullOrWhiteSpace(messageIncoming.Header?.Topic?.Value))
                    {
                        
                        _logger.LogWarning("No Topic provided in Header.");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(_hubConnection?.ConnectionId))
                    {
                        _logger.LogWarning("No connection id found. No message will be processed.");
                        return;
                    }

                    var consumerSearchResult = _consumerResolver.ResolveConsumerByTopic(topicType, messageIncoming.Content?.Json);

                    if (consumerSearchResult.Error)
                    {
                        _logger.LogError(consumerSearchResult.Exception, consumerSearchResult.Exception?.Message);
                        return;
                    }
                    
                    var responseMessage = await _httpClient.PostAsJsonAsync($"/consume",
                        new ConsumedMessage(messageIncoming,
                            _currentService),
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    responseMessage.EnsureSuccessStatusCode();

                    consumerSearchResult.ProcessMethod?.Invoke(consumerSearchResult.Consumer, new[] { consumerSearchResult.DeserializedEntity });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    await SendError(messageIncoming, ex, cancellationToken).ConfigureAwait(false);
                }
            });
        }

        await CheckHubConnectionStateAndStartIt(cancellationToken).ConfigureAwait(false);
    }
   
    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection?.InvokeAsync("RemoveConnectionIds", _currentService)!;
            await _hubConnection.DisposeAsync().ConfigureAwait(false);
        }
    }

    #region PrivateMethods

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

    private async Task SendError(Message? message, Exception exception, CancellationToken cancellationToken = default)
    {
        try
        {
            var responseMessage = await _httpClient.PostAsJsonAsync("/error",
                new ErrorMessage(message,
                    _currentService, exception, _retryPolicy),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            responseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    #endregion
}