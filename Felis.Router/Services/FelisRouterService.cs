using Felis.Core;
using Felis.Router.Hubs;
using Felis.Router.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Services;

public sealed class FelisRouterService : IFelisRouterService
{
    private readonly IHubContext<FelisRouterHub> _hubContext;
    private readonly ILogger<FelisRouterService> _logger;
    private readonly IFelisRouterStorage _storage;
    private readonly string _topic = "NewDispatchedMethod";

    public FelisRouterService(IHubContext<FelisRouterHub> hubContext, ILogger<FelisRouterService> logger, IFelisRouterStorage storage)
    {
        _hubContext = hubContext;
        _logger = logger;
        _storage = storage;
    }

    public async Task<bool> Dispatch(Message message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

			if (message.Topic == null)
			{
				throw new ArgumentNullException(nameof(message.Topic));
			}

			if (string.IsNullOrWhiteSpace(message.Topic.Value))
			{
				throw new ArgumentNullException(nameof(message.Topic.Value));
			}

			_storage.MessageAdd(message);

            //dispatch it
            await _hubContext.Clients.All.SendAsync(_topic, message, cancellationToken).ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public Task<bool> Consume(ConsumedMessage consumedMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            if (consumedMessage == null)
            {
                throw new ArgumentNullException(nameof(consumedMessage));
            }

            _storage.ConsumedMessageAdd(consumedMessage);
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Task.FromResult(false);
        }
    }
}