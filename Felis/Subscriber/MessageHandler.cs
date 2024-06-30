using System.Net.Http.Json;
using System.Text.Json;
using Felis.Common.Models;
using Felis.Subscriber.Resolvers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Felis.Subscriber;

public sealed class MessageHandler : IAsyncDisposable
{
    private readonly HubConnection? _hubConnection;
    private readonly ILogger<MessageHandler> _logger;
    private List<TopicValue>? _topics;
    private List<QueueValue>? _queues;
    private readonly HttpClient _httpClient;
    private readonly ConsumerResolver _consumerResolver;
    private readonly SubscriberResolver _subscriberResolver;

    public MessageHandler(HubConnection? hubConnection, ILogger<MessageHandler> logger, HttpClient httpClient,
        IServiceScopeFactory serviceScopeFactory)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);
        var scope = serviceScopeFactory.CreateScope();
        _consumerResolver = scope.ServiceProvider.GetService<ConsumerResolver>() ??
                            throw new ArgumentNullException(nameof(ConsumerResolver));
        _subscriberResolver = scope.ServiceProvider.GetService<SubscriberResolver>() ??
                              throw new ArgumentNullException(nameof(SubscriberResolver));
    }

    public async Task PublishAsync<T>(T payload, string? topic, CancellationToken cancellationToken = default)
        where T : class
    {
        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentNullException(nameof(topic));
        }

        try
        {
            await CheckHubConnectionStateAndStartIt(cancellationToken);

            var json = JsonSerializer.Serialize(payload);

            var responseMessage = await _httpClient.PostAsJsonAsync($"/messages/dispatch",
                new MessageRequest(Guid.NewGuid(), topic, string.Empty, json),
                cancellationToken: cancellationToken);

            responseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
    
    public async Task EnqueueAsync<T>(T payload, string? queue, CancellationToken cancellationToken = default)
        where T : class
    {
        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (string.IsNullOrWhiteSpace(queue))
        {
            throw new ArgumentNullException(nameof(queue));
        }

        try
        {
            await CheckHubConnectionStateAndStartIt(cancellationToken);

            var json = JsonSerializer.Serialize(payload);

            var responseMessage = await _httpClient.PostAsJsonAsync($"/messages/enqueue",
                new MessageRequest(Guid.NewGuid(), string.Empty, queue, json),
                cancellationToken: cancellationToken);

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

        SubscribeTopics(cancellationToken);
        SubscribeQueues(cancellationToken);

        await CheckHubConnectionStateAndStartIt(cancellationToken);
    }

    private void SubscribeTopics(CancellationToken cancellationToken)
    {
        var topicsTypes = _subscriberResolver.GetTypesForTopics();

        _topics = topicsTypes.Select(x => x.Key).ToList();

        foreach (var topicType in topicsTypes)
        {
            if (string.IsNullOrWhiteSpace(topicType.Key.Name))
            {
                continue;
            }

            _hubConnection?.On<Message?>(topicType.Key.Name, async (messageIncoming) =>
            {
                try
                {
                    if (messageIncoming == null)
                    {
                        _logger.LogWarning("No message incoming.");
                        return;
                    }

                    if (messageIncoming.Header?.Topic == null ||
                        string.IsNullOrWhiteSpace(messageIncoming.Header?.Topic))
                    {
                        _logger.LogWarning("No Topic provided in Header.");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(_hubConnection?.ConnectionId))
                    {
                        _logger.LogWarning("No connection id found. No message will be processed.");
                        return;
                    }

                    var consumerSearchResult =
                        _subscriberResolver.ResolveSubscriberByTopic(topicType, messageIncoming.Content?.Payload);

                    if (consumerSearchResult.Error)
                    {
                        _logger.LogError(consumerSearchResult.Exception, consumerSearchResult.Exception?.Message);
                        return;
                    }

                    var responseMessage = await _httpClient.PostAsJsonAsync(
                        "/messages/consume",
                        new ConsumedMessage(messageIncoming.Header!.Id, _hubConnection.ConnectionId,
                            new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()),
                        cancellationToken: cancellationToken);

                    responseMessage.EnsureSuccessStatusCode();

#pragma warning disable CS4014
                    Task.Run(() =>
#pragma warning restore CS4014
                    {
                        var startTimeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

                        try
                        {
                            consumerSearchResult.ProcessMethod?.Invoke(consumerSearchResult.Consumer,
                                new[] { consumerSearchResult.DeserializedEntity });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.InnerException, ex.InnerException?.Message);
                        }
                        finally
                        {
                            var endTimeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

                            _logger.LogDebug($"Processed message {messageIncoming.Header?.Id} in {endTimeStamp - startTimeStamp} ms");
                        }
                    }, cancellationToken);
                }
                catch (Exception? ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            });
        }
    }
    
     private void SubscribeQueues(CancellationToken cancellationToken)
    {
        var queueTypes = _consumerResolver.GetTypesForQueues();

        _queues = queueTypes.Select(x => x.Key).ToList();

        foreach (var queueType in queueTypes)
        {
            if (string.IsNullOrWhiteSpace(queueType.Key.Name))
            {
                continue;
            }

            _hubConnection?.On<Message?>(queueType.Key.Name, async (messageIncoming) =>
            {
                try
                {
                    if (messageIncoming == null)
                    {
                        _logger.LogWarning("No message incoming.");
                        return;
                    }

                    if (messageIncoming.Header?.Topic == null ||
                        string.IsNullOrWhiteSpace(messageIncoming.Header?.Topic))
                    {
                        _logger.LogWarning("No Topic provided in Header.");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(_hubConnection?.ConnectionId))
                    {
                        _logger.LogWarning("No connection id found. No message will be processed.");
                        return;
                    }

                    var consumerSearchResult =
                        _consumerResolver.ResolveConsumerByQueue(queueType, messageIncoming.Content?.Payload);

                    if (consumerSearchResult.Error)
                    {
                        _logger.LogError(consumerSearchResult.Exception, consumerSearchResult.Exception?.Message);
                        return;
                    }

                    var responseMessage = await _httpClient.PostAsJsonAsync(
                        "/messages/ack",
                        new ConsumedMessage(messageIncoming.Header!.Id, _hubConnection.ConnectionId,
                            new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()),
                        cancellationToken: cancellationToken);

                    responseMessage.EnsureSuccessStatusCode();

#pragma warning disable CS4014
                    Task.Run(async () =>
#pragma warning restore CS4014
                    {
                        var startTimeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

                        try
                        {
                            consumerSearchResult.ProcessMethod?.Invoke(consumerSearchResult.Consumer,
                                new[] { consumerSearchResult.DeserializedEntity });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.InnerException, ex.InnerException?.Message);
                            await SendNack(messageIncoming, ex.InnerException, cancellationToken);
                        }
                        finally
                        {
                            var endTimeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

                            _logger.LogDebug($"Processed message {messageIncoming.Header?.Id} in {endTimeStamp - startTimeStamp} ms");
                        }
                    }, cancellationToken);
                }
                catch (Exception? ex)
                {
                    _logger.LogError(ex, ex.Message);
                    await SendNack(messageIncoming, ex, cancellationToken);
                }
            });
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.InvokeAsync("RemoveConnectionId", _hubConnection.ConnectionId);
            await _hubConnection.DisposeAsync();
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

            await _hubConnection?.InvokeAsync("SetConnectionId", _topics, _queues, cancellationToken)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    private async Task SendNack(Message? message, Exception? exception, CancellationToken cancellationToken = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(_hubConnection);
            ArgumentException.ThrowIfNullOrWhiteSpace(_hubConnection.ConnectionId);

            var responseMessage = await _httpClient.PostAsJsonAsync("/messages/nack",
                new ErrorMessageRequest(message!.Header!.Id,
                    _hubConnection.ConnectionId, new ErrorDetail(exception?.Message, exception?.StackTrace)),
                cancellationToken: cancellationToken);

            responseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
 
    #endregion
}