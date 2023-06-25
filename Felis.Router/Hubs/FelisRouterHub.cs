using Felis.Core;
using Felis.Core.Models;
using Felis.Router.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Hubs;

[Route("/felis/router")]
internal sealed class FelisRouterHub : Hub
{
    private readonly ILogger<FelisRouterHub> _logger;
    private readonly IFelisRouterStorage _felisRouterStorage;
    private readonly IFelisConnectionManager _felisConnectionManager;
    private readonly string _topic = "NewDispatchedMethod";

    public FelisRouterHub(ILogger<FelisRouterHub> logger, IFelisRouterStorage felisRouterStorage,
        IFelisConnectionManager felisConnectionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _felisRouterStorage = felisRouterStorage ?? throw new ArgumentNullException(nameof(felisRouterStorage));
        _felisConnectionManager =
            felisConnectionManager ?? throw new ArgumentNullException(nameof(felisConnectionManager));
    }

    public async Task<bool> Dispatch(Message? message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message.Header?.Topic == null)
            {
                throw new ArgumentNullException($"Topic non provided in Header");
            }

            if (string.IsNullOrWhiteSpace(message.Header?.Topic?.Value))
            {
                throw new ArgumentNullException($"Topic Value non provided in Header");
            }

            var result = _felisRouterStorage.MessageAdd(message);

            if (!result)
            {
                _logger.LogWarning("Cannot add message in storage.");
            }

            if (message.Header.Services != null && message.Header.Services.Any())
            {
                foreach (var service in message.Header.Services)
                {
                    var connectionIds = GetConnectionIds(service);

                    if (!connectionIds.Any())
                    {
                        continue;
                    }

                    foreach (var connectionId in connectionIds)
                    {
                        await Clients.Client(connectionId).SendAsync(_topic, message, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await Clients.All.SendAsync(_topic, message, cancellationToken).ConfigureAwait(false);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public Task<bool> Consume(ConsumedMessage? consumedMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (consumedMessage == null)
            {
                throw new ArgumentNullException(nameof(consumedMessage));
            }

            var result = _felisRouterStorage.ConsumedMessageAdd(consumedMessage);

            if (!result)
            {
                _logger.LogWarning("Cannot add consumed message in storage.");
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Task.FromResult(false);
        }
    }

    public string SetConnectionId(Service service)
    {
        _felisConnectionManager.KeepServiceConnection(service,
            Context.ConnectionId);
        return Context.ConnectionId;
    }

    public void RemoveConnectionIds(Service service)
    {
        _felisConnectionManager.RemoveServiceConnections(service);
    }

    public List<string> GetConnectionIds(Service service)
    {
        return _felisConnectionManager.GetServiceConnections(service);
    }
}