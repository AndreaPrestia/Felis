using System.Net.Http.Json;
using System.Text.Json;
using Felis.Core;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Felis.Client;

public sealed class MessageHandler
{
    private readonly HubConnection? _hubConnection;
    private readonly ILogger<MessageHandler> _logger;
    private readonly FelisConfiguration _configuration;

    public MessageHandler(HubConnection? hubConnection, ILogger<MessageHandler> logger, FelisConfiguration configuration)
    {
        _hubConnection = hubConnection;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Publish<T>(T payload, string? topic, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            await CheckHubConnectionStateAndStartIt(cancellationToken);

            //TODO add an authorization token as parameter

            using var client = new HttpClient();
            var responseMessage = await client.PostAsJsonAsync($"{_configuration.RouterEndpoint}/dispatch",
                Message.From(topic ?? payload.GetType().FullName, payload));

            responseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    public async Task<T?> Consume<T>(string topic, CancellationToken cancellationToken = default) where T : class
    {
        var message = default(T);

        _hubConnection!.On<string, string>(topic, async (_, msg) =>
        {
            try
            {
                var messageIncoming = JsonSerializer.Deserialize<Message>(msg);

                if (messageIncoming == null)
                {
                    throw new ArgumentNullException(nameof(messageIncoming));
                }

                if (string.IsNullOrWhiteSpace(_hubConnection?.ConnectionId))
                {
                    throw new ArgumentNullException(nameof(_hubConnection.ConnectionId));
                }

                message = messageIncoming.Content as T;

                if (message == null)
                {
                    throw new ArgumentException(
                        $"Cannot deserialize Content to type specified in consume {typeof(T).Name}");
                }

                using var client = new HttpClient();
                var responseMessage = await client.PostAsJsonAsync($"{_configuration.RouterEndpoint}/consume",
                    ConsumedMessage.From(messageIncoming, Guid.Parse(_hubConnection.ConnectionId)));

                responseMessage.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        });

        await CheckHubConnectionStateAndStartIt(cancellationToken);

        return message;
    }

    private async Task CheckHubConnectionStateAndStartIt(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_hubConnection?.State == HubConnectionState.Disconnected)
            {
                await _hubConnection?.StartAsync(cancellationToken)!;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
}