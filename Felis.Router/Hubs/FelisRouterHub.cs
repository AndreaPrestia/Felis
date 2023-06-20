﻿using Felis.Core;
using Felis.Router.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Hubs;

[Route("/felis/router")]
public sealed class FelisRouterHub : Hub
{
    private readonly ILogger<FelisRouterHub> _logger;
    private readonly IFelisRouterStorage _felisRouterStorage;
    private readonly IFelisConnectionManager _felisConnectionManager;
    public FelisRouterHub(ILogger<FelisRouterHub> logger, IFelisRouterStorage felisRouterStorage, IFelisConnectionManager felisConnectionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _felisRouterStorage = felisRouterStorage ?? throw new ArgumentNullException(nameof(felisRouterStorage));
        _felisConnectionManager = felisConnectionManager ?? throw new ArgumentNullException(nameof(felisConnectionManager));
    }

    public async Task<bool> Dispatch(Message message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _felisRouterStorage.MessageAdd(message);

            await Clients.All.SendAsync(message.Topic, message.Content, cancellationToken).ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public Task<bool> Consume(ConsumedMessage consumedMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (consumedMessage == null)
            {
                throw new ArgumentNullException(nameof(consumedMessage));
            }

            _felisRouterStorage.ConsumedMessageAdd(consumedMessage);
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Task.FromResult(false);
        }
    }

	public string SetConnectionId(Guid id)
	{
		_felisConnectionManager.KeepUserConnection(id,
			Context.ConnectionId);
		return Context.ConnectionId;
	}
}