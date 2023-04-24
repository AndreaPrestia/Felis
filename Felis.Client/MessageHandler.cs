using System.Text.Json;
using Felis.Core;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Felis.Client;

public class MessageHandler
{
    private readonly HubConnection? _hubConnection;
    private readonly ILogger<MessageHandler> _logger;
    private static string OnNewMessageMethod = "OnNewMessage";
    private static string OnMessageStatus = "OnMessageStatus";

    public MessageHandler(HubConnection? hubConnection, ILogger<MessageHandler> logger)
    {
        _hubConnection = hubConnection;
        _logger = logger;
    }

    public async Task Publish<T>(T payload, string? topic, CancellationToken cancellationToken = default)
    {
        try
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            //TODO add an authorization token as parameter
            await _hubConnection?.InvokeAsync(OnNewMessageMethod, Message.From(topic ?? payload.GetType().FullName, payload), cancellationToken)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    public async Task<Message?> Consume(CancellationToken cancellationToken = default)
    {
        Message? message = null;

        _hubConnection!.On<string, string>(OnMessageStatus, (_, msg) =>
        {
            try
            {
                message = JsonSerializer.Deserialize<Message>(msg);

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }

                if (string.IsNullOrWhiteSpace(_hubConnection?.ConnectionId))
                {
                    throw new ArgumentNullException(nameof(_hubConnection.ConnectionId));
                }

                _hubConnection?.InvokeAsync(OnMessageStatus,
                        ConsumedMessage.From(message, Guid.Parse(_hubConnection.ConnectionId)), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        });

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

        return message;
    }
}