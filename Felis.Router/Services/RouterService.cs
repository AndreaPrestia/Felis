using Felis.Core.Models;
using Felis.Router.Managers;
using Felis.Router.Storage;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Services;

internal sealed class RouterService
{
    private readonly ILogger<RouterService> _logger;
    private readonly RouterStorage _storage;
    private readonly ConnectionManager _felisConnectionManager;
    private readonly HttpInstanceService _httpInstanceService;

    public RouterService(ILogger<RouterService> logger, RouterStorage storage, ConnectionManager felisConnectionManager, HttpInstanceService httpInstanceService)
    {
        _logger = logger;
        _storage = storage;
        _felisConnectionManager = felisConnectionManager;
        _httpInstanceService = httpInstanceService;
    }

    public bool Dispatch(Topic? topic, Message? message)
    {
        try
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (message.Header?.Topic == null)
            {
                throw new ArgumentException("No Topic provided in Header");
            }

            var topicValue = message.Header?.Topic?.Value;

            if (string.IsNullOrWhiteSpace(topicValue))
            {
                throw new ArgumentException("No Topic Value provided in Header");
            }

            if (!string.Equals(topicValue, topic?.Value, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException("The topic provided in message and route are not matching");
            }

			message.Header!.Origin = _httpInstanceService.GetCurrentOrigin();

			var result = _storage.ReadyMessageAdd(message);

            if (!result)
            {
                _logger.LogWarning("Cannot add message in storage.");
                return result;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public bool Consume(Guid id, ConsumedMessage? consumedMessage)
    {
        try
        {
            if (consumedMessage == null)
            {
                throw new ArgumentNullException(nameof(consumedMessage));
            }
            
            if (consumedMessage.Message?.Header?.Id != id)
            {
                throw new InvalidOperationException("The id provided in message and route are not matching");
            }

            var result = _storage.ConsumedMessageAdd(consumedMessage);

            if (!result)
            {
                _logger.LogWarning("Cannot add consumed message in storage.");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public bool Error(Guid id, ErrorMessage? errorMessage)
    {
        try
        {
            if (errorMessage == null)
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }

            if (errorMessage.Message?.Header?.Id != id)
            {
                throw new InvalidOperationException("The id provided in message and route are not matching");
            }
            
            var result = _storage.ErrorMessageAdd(errorMessage);

            if (!result)
            {
                _logger.LogWarning("Cannot add error message in storage.");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public bool PurgeReady(Topic? topic)
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

            return _storage.ReadyMessagePurge(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public List<Consumer> Consumers(Topic? topic)
    {
        try
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }
            
            return _felisConnectionManager.GetConnectedConsumers(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<Consumer>();
        }
    }

    public List<Message?> ReadyMessageList(Topic? topic = null)
    {
        try
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }
            
            return _storage.ReadyMessageList(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<Message?>();
        }
    }

    public List<Message?> SentMessageList(Topic? topic = null)
    {
        try
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            return _storage.SentMessageList(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<Message?>();
        }
    }

    public List<ErrorMessage?> ErrorMessageList(Topic? topic = null)
    {
        try
        {
            return _storage.ErrorMessageList(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<ErrorMessage?>();
        }
    }

    public List<ConsumedMessage?> ConsumedMessageList(ConnectionId connectionId)
    {
        try
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }
            
            return _storage.ConsumedMessageList(connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<ConsumedMessage?>();
        }
    }

    public List<ConsumedMessage?> ConsumedMessageList(Topic topic)
    {
        try
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }
            
            return _storage.ConsumedMessageList(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<ConsumedMessage?>();
        }
    }
    
    public List<ConsumedMessage?> ConsumedMessageList(ConnectionId connectionId, Topic topic)
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
            
            return _storage.ConsumedMessageList(connectionId, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<ConsumedMessage?>();
        }
    }
}