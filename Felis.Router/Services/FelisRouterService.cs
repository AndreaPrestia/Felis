using Felis.Core;
using Felis.Core.Models;
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
    private readonly IFelisConnectionManager _felisConnectionManager;

    public FelisRouterService(IHubContext<FelisRouterHub> hubContext, ILogger<FelisRouterService> logger,
        IFelisRouterStorage storage, IFelisConnectionManager felisConnectionManager)
    {
        _hubContext = hubContext;
        _logger = logger;
        _storage = storage;
        _felisConnectionManager = felisConnectionManager;
    }

    public async Task<bool> Dispatch(Message? message, CancellationToken cancellationToken = default)
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
            if (message.ServiceHosts != null && message.ServiceHosts.Any())
            {
                var connectedServices = _felisConnectionManager.GetConnectedServices();
                
                if (!connectedServices.Any())
                {
                    _logger.LogWarning("No connected services to dispatch. The message won't be published");
                    return false;
                }

                if (!connectedServices.Select(x => x.Host).Intersect(message.ServiceHosts).Any())
                {
                    _logger.LogWarning(
                        "No connected services available in the list provided to dispatch. The message won't be published");
                    return false;
                }

                foreach (var serviceHost in message.ServiceHosts)
                {
                    await _hubContext.Clients.Client(serviceHost).SendAsync(_topic, message, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                await _hubContext.Clients.All.SendAsync(_topic, message, cancellationToken).ConfigureAwait(false);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public Task<bool> Consume(ConsumedMessage? consumedMessage, CancellationToken cancellationToken = default)
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

    public Task<bool> Error(ErrorMessage? errorMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            if (errorMessage == null)
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }

            _storage.ErrorMessageAdd(errorMessage);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Task.FromResult(false);
        }
    }

    public Task<List<Service>> GetConnectedServices(CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(_felisConnectionManager.GetConnectedServices());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Task.FromResult(new List<Service>());
        }
    }
}