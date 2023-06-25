using Felis.Core;
using Felis.Core.Models;
using Felis.Router.Hubs;
using Felis.Router.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Services;

internal sealed class FelisRouterService : IFelisRouterService
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

            if (message.Header?.Topic == null)
            {
                throw new ArgumentNullException($"No Topic provided in Header");
            }

            if (string.IsNullOrWhiteSpace(message.Header?.Topic?.Value))
            {
                throw new ArgumentNullException($"No Topic Value provided in Header");
            }

            var result = _storage.MessageAdd(message);

            if (!result)
            {
                _logger.LogWarning("Cannot add message in storage.");
                return result;
            }

            //dispatch it
            if (message.Header?.Services != null && message.Header.Services.Any())
            {
                var connectedServices = _felisConnectionManager.GetConnectedServices();

                if (!connectedServices.Any())
                {
                    _logger.LogWarning("No connected services to dispatch. The message won't be published");
                    return false;
                }

                if (!connectedServices.Intersect(message.Header.Services).Any())
                {
                    _logger.LogWarning(
                        "No connected services available in the list provided to dispatch. The message won't be published");
                    return false;
                }

                foreach (var service in message.Header.Services)
                {
                    var connectionIds = _felisConnectionManager.GetServiceConnections(service);

                    if (!connectionIds.Any())
                    {
                        continue;
                    }

                    foreach (var connectionId in connectionIds)
                    {
                        await _hubContext.Clients.Client(connectionId).SendAsync(_topic, message, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await _hubContext.Clients.All.SendAsync(_topic, message, cancellationToken).ConfigureAwait(false);
            }

            return result;
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

            var result = _storage.ConsumedMessageAdd(consumedMessage);

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

    public Task<bool> Error(ErrorMessage? errorMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            if (errorMessage == null)
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }

            var result = _storage.ErrorMessageAdd(errorMessage);

            if (!result)
            {
                _logger.LogWarning("Cannot add error message in storage.");
            }

            return Task.FromResult(result);
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