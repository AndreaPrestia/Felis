using System.Text.Json;
using Felis.Core;
using Felis.Router.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Hubs;

public class FelisRouterHub : IFelisRouterHub
{
    private readonly HubConnection? _hubConnection;
    private readonly ILogger<FelisRouterHub> _logger;
    private readonly IFelisRouterStorage _felisRouterStorage;
    private static string OnNewMessageMethod = "OnNewMessage";
    private static string OnMessageStatus = "OnMessageStatus";
    
    public FelisRouterHub(HubConnection? hubConnection, ILogger<FelisRouterHub> logger, IFelisRouterStorage felisRouterStorage)
    {
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _felisRouterStorage = felisRouterStorage ?? throw new ArgumentNullException(nameof(felisRouterStorage));
    }

    public async void ListenForNewMessages(CancellationToken cancellationToken = default)
    {
        _hubConnection!.On<string, string>(OnNewMessageMethod, (_, msg) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<Message>(msg);

                if (message == null)
                {
                    throw new ArgumentNullException(nameof(message));
                }
                
                //TODO validate client
                _felisRouterStorage.MessageAdd(message);
                
                //TODO add dispatch only for client connected
                //dispatch it
                _hubConnection?.InvokeAsync(message.Topic, message.Content, cancellationToken).ConfigureAwait(false);
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

    }

    public async void ListenForMessageStatus(CancellationToken cancellationToken = default)
    {
        _hubConnection!.On<string, string>(OnMessageStatus, (_, msg) =>
        {
            try
            {
                var consumedMessage = JsonSerializer.Deserialize<ConsumedMessage>(msg);

                if (consumedMessage == null)
                {
                    throw new ArgumentNullException(nameof(consumedMessage));
                }

                //TODO validate client

                _felisRouterStorage.ConsumedMessageAdd(consumedMessage);
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
    }
}