using Felis.Core.Models;
using Felis.Router.Abstractions;
using Felis.Router.Entities;
using Felis.Router.Managers;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Services;

internal sealed class MessageService
{
    private readonly ILogger<MessageService> _logger;
    private readonly IRouterStorage _storage;
    private readonly ConnectionManager _connectionManager;
    private readonly QueueService _queueService;

    public MessageService(ILogger<MessageService> logger, IRouterStorage storage, ConnectionManager connectionManager, QueueService queueService)
    {
        _logger = logger;
        _storage = storage;
        _connectionManager = connectionManager;
        _queueService = queueService;
    }

    public MessageStatus Dispatch(string? topic, MessageRequest? message)
    {
        try
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.IsNullOrWhiteSpace(message.Topic))
            {
                throw new ArgumentNullException($"No Topic provided");
            }

            if (!string.Equals(message.Topic, topic, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException("The topic provided in message and route are not matching");
            }

            var result = _storage.ReadyMessageAdd(message);

            if (result == MessageStatus.Error)
            {
                _logger.LogWarning("Cannot add message in storage.");
                return result;
            }

            _queueService.Enqueue(message.Id);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return MessageStatus.Error;
        }
    }

    public MessageStatus Consume(Guid id, ConsumedMessage? consumedMessage)
    {
        try
        {
            if (consumedMessage == null)
            {
                throw new ArgumentNullException(nameof(consumedMessage));
            }

            if (consumedMessage.Id != id)
            {
                throw new InvalidOperationException("The id provided in message and route are not matching");
            }

            if (consumedMessage.Id != id)
            {
                throw new InvalidOperationException("The id provided in message and route are not matching");
            }

            var result = _storage.ConsumedMessageAdd(consumedMessage);

            if (result == MessageStatus.Error)
            {
                _logger.LogWarning("Cannot add consumed message in storage.");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return MessageStatus.Error;
        }
    }

    public MessageStatus Error(Guid id, ErrorMessageRequest? errorMessage)
    {
        try
        {
            if (errorMessage == null)
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }

            if (errorMessage.Id != id)
            {
                throw new InvalidOperationException("The id provided in message and route are not matching");
            }
            
            var result = _storage.ErrorMessageAdd(errorMessage);

            if (result == MessageStatus.Error)
            {
                _logger.LogWarning("Cannot add error message in storage.");
            }
    
            if (result == MessageStatus.Ready)
            {
                _queueService.Enqueue(id);
                _logger.LogDebug($"Re-enqueued message {id}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return MessageStatus.Error;
        }
    }

    public MessageStatus Process(Guid id, ProcessedMessage? processedMessage)
    {
        try
        {
            if (processedMessage == null)
            {
                throw new ArgumentNullException(nameof(processedMessage));
            }

            if (processedMessage.Id != id)
            {
                throw new InvalidOperationException("The id provided in message and route are not matching");
            }

            if (processedMessage.Id != id)
            {
                throw new InvalidOperationException("The id provided in message and route are not matching");
            }

            var result = _storage.ProcessedMessageAdd(processedMessage);

            if (result == MessageStatus.Error)
            {
                _logger.LogWarning("Cannot add processed message in storage.");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return MessageStatus.Error;
        }
    }

    public int PurgeReady(string? topic)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentNullException(nameof(topic));
            }

            return _storage.ReadyMessagePurge(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return -1;
        }
    }

    public List<Consumer> Consumers(string? topic)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentNullException(nameof(topic));
            }
            
            return _connectionManager.GetConnectedConsumers(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<Consumer>();
        }
    }

    public List<Message> ReadyMessageList(string? topic = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentNullException(nameof(topic));
            }
            
            return _storage.ReadyMessageList(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<Message>();
        }
    }

    public List<Message> SentMessageList(string? topic = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentNullException(nameof(topic));
            }

            return _storage.SentMessageList(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<Message>();
        }
    }

    public List<ErrorMessage> ErrorMessageList(string? topic = null)
    {
        try
        {
            return _storage.ErrorMessageList(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<ErrorMessage>();
        }
    }

    public List<ConsumedMessage> ConsumedMessageList(string topic)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentNullException(nameof(topic));
            }
            
            return _storage.ConsumedMessageList(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<ConsumedMessage>();
        }
    }
    
    public List<ConsumedMessage> ConsumedMessageList(string connectionId, string topic)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                throw new ArgumentNullException(nameof(connectionId));
            }
            
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentNullException(nameof(topic));
            }
            
            return _storage.ConsumedMessageList(connectionId, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<ConsumedMessage>();
        }
    }
}