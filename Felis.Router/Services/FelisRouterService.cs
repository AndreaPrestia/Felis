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
    private readonly IFelisConnectionManager _felisConnectionManager;

    public FelisRouterService(IHubContext<FelisRouterHub> hubContext, ILogger<FelisRouterService> logger,
        IFelisRouterStorage storage, IFelisConnectionManager felisConnectionManager)
    {
        _hubContext = hubContext;
        _logger = logger;
        _storage = storage;
        _felisConnectionManager = felisConnectionManager;
    }

    public async Task<bool> Dispatch(Topic topic, Message? message, CancellationToken cancellationToken = default)
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

            var topicValue = message.Header?.Topic?.Value;

            if (string.IsNullOrWhiteSpace(topicValue))
            {
                throw new ArgumentNullException($"No Topic Value provided in Header");
            }

            if (!string.Equals(topicValue, topic.Value, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException("The topic provided in message and route are not matching");
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
                        if (string.IsNullOrWhiteSpace(connectionId.Value))
                        {
                            continue;
                        }
                        
                        await _hubContext.Clients.Client(connectionId.Value).SendAsync(topicValue, message, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await _hubContext.Clients.All.SendAsync(topicValue, message, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public Task<bool> Consume(Guid id, ConsumedMessage? consumedMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            if (consumedMessage == null)
            {
                throw new ArgumentNullException(nameof(consumedMessage));
            }
            
            if (consumedMessage.Message?.Id != id)
            {
                throw new InvalidOperationException("The id provided in message and route are not matching");
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

    public Task<bool> Error(Guid id, ErrorMessage? errorMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            if (errorMessage == null)
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }

            if (errorMessage.Message?.Id != id)
            {
                throw new InvalidOperationException("The id provided in message and route are not matching");
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

    public Task<bool> Purge(Topic? topic, CancellationToken cancellationToken = default)
    {
        try
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (string.IsNullOrWhiteSpace(topic.Value))
            {
                throw new ArgumentNullException(nameof(topic.Value));
            }

            return Task.FromResult(_storage.MessagePurge(topic));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Task.FromResult(false);
        }
    }

    public Task<List<Service>> Consumers(Topic? topic, CancellationToken cancellationToken = default)
    {
        try
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }
            
            return Task.FromResult(_felisConnectionManager.GetConnectedServices().Where(x => x.Topics.Select(t => t.Value).ToList().Contains(topic.Value)).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Task.FromResult(new List<Service>());
        }
    }

    public Task<List<Message?>> MessageList(Topic? topic = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }
            
            return Task.FromResult(_storage.MessageList(topic));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Task.FromResult(new List<Message?>());
        }
    }

    public Task<List<ErrorMessage>> ErrorMessageList(Topic? topic = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return Task.FromResult(_storage.ErrorMessageList(topic));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Task.FromResult(new List<ErrorMessage>());
        }
    }

    public Task<List<ConsumedMessage?>> ConsumedMessageList(ConnectionId connectionId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }
            
            return Task.FromResult(_storage.ConsumedMessageList(connectionId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Task.FromResult(new List<ConsumedMessage?>());
        }
    }

    public Task<List<ConsumedMessage?>> ConsumedMessageList(Topic topic, CancellationToken cancellationToken = default)
    {
        try
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }
            
            return Task.FromResult(_storage.ConsumedMessageList(topic));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Task.FromResult(new List<ConsumedMessage?>());
        }
    }
    
    public Task<List<ConsumedMessage?>> ConsumedMessageList(ConnectionId connectionId, Topic topic, CancellationToken cancellationToken = default)
    {
        try
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }
            
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }
            
            return Task.FromResult(_storage.ConsumedMessageList(connectionId, topic));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Task.FromResult(new List<ConsumedMessage?>());
        }
    }

}