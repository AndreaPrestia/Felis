﻿using System.Net.Http.Json;
using System.Text.Json;
using Felis.Client.Resolvers;
using Felis.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Felis.Client;

public sealed class MessageHandler : IAsyncDisposable
{
    private readonly HubConnection? _hubConnection;
    private readonly ILogger<MessageHandler> _logger;
    private List<Topic>? _topics;
    private readonly HttpClient _httpClient;
    private RetryPolicy? _retryPolicy;
    private string? _friendlyName;
    private readonly ConsumerResolver _consumerResolver;

    public MessageHandler(HubConnection? hubConnection, ILogger<MessageHandler> logger,HttpClient httpClient, IServiceScopeFactory serviceScopeFactory)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);
        var scope = serviceScopeFactory.CreateScope();
        _consumerResolver = scope.ServiceProvider.GetService<ConsumerResolver>() ?? throw new ArgumentNullException(nameof(ConsumerResolver));
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
            await CheckHubConnectionStateAndStartIt(cancellationToken).ConfigureAwait(false);

            //TODO add an authorization token as parameter

            var json = JsonSerializer.Serialize(payload);

            var responseMessage = await _httpClient.PostAsJsonAsync($"/messages/{topic}/dispatch",
                new Message(new Header(Guid.NewGuid(), new Topic(topic)), new Content(json)),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            responseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    public async Task SubscribeAsync(string friendlyName, RetryPolicy? retryPolicy, CancellationToken cancellationToken = default)
    {
        if (_hubConnection == null)
        {
            throw new ArgumentNullException($"Connection to Felis router not correctly initialized");
        }

        _retryPolicy = retryPolicy;
        _friendlyName = friendlyName;
        
        var topicsTypes = _consumerResolver.GetTypesForTopics();

        _topics = topicsTypes.Select(x => x.Key).ToList();

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
                    
                    var responseMessage = await _httpClient.PostAsJsonAsync($"/messages/{messageIncoming.Header?.Id}/consume",
                        new ConsumedMessage(messageIncoming,
                            new ConnectionId(_hubConnection.ConnectionId)),
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    responseMessage.EnsureSuccessStatusCode();

#pragma warning disable CS4014
                    Task.Run(async () =>
#pragma warning restore CS4014
                    {
                        try
                        {
                            consumerSearchResult.ProcessMethod?.Invoke(consumerSearchResult.Consumer, new[] { consumerSearchResult.DeserializedEntity });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex.InnerException, ex.InnerException?.Message);
                            await SendError(messageIncoming, ex.InnerException, cancellationToken).ConfigureAwait(false);
                        }
                    }, cancellationToken);
                }
                catch (Exception? ex)
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
            await _hubConnection?.InvokeAsync("RemoveConnectionId", new ConnectionId(_hubConnection?.ConnectionId))!;
            await _hubConnection!.DisposeAsync().ConfigureAwait(false);
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

            await _hubConnection?.InvokeAsync("SetConnectionId", _topics, _friendlyName, cancellationToken)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    private async Task SendError(Message? message, Exception? exception, CancellationToken cancellationToken = default)
    {
        try
        {
            var responseMessage = await _httpClient.PostAsJsonAsync($"/messages/{message?.Header?.Id}/error",
                new ErrorMessage(message,
                    new ConnectionId(_hubConnection?.ConnectionId), new ErrorDetail(exception?.Message, exception?.StackTrace), _retryPolicy),
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