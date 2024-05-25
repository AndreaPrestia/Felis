using Felis.Core.Models;
using Felis.Router.Enums;
using Felis.Router.Services;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Managers;

public sealed class RouterManager
{
    private readonly ILogger<RouterManager> _logger;
    private readonly ConnectionService _connectionService;
    private readonly MessageService _messageService;
    private readonly QueueService _queueService;

    internal RouterManager(ILogger<RouterManager> logger, MessageService messageService, ConnectionService connectionService, QueueService queueService)
    {
        _logger = logger;
        _messageService = messageService;
        _connectionService = connectionService;
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

            var result = _messageService.Add(message);

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

            var result = _messageService.Consume(consumedMessage);

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

            var result = _messageService.Error(errorMessage);

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

            var result = _messageService.Process(processedMessage);

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

    public int Purge(string? topic)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentNullException(nameof(topic));
            }

            return _messageService.Purge(topic);
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

            return _connectionService.GetConnectedConsumers(topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<Consumer>();
        }
    }

    public List<Message> ReadyList(string? topic = null) => _messageService.ReadyList(topic);

    public List<Message> SentList(string? topic = null) => _messageService.SentList(topic);

    public List<ErrorMessage> ErrorList(string? topic = null) => _messageService.ErrorList(topic);

    public List<ConsumedMessage> ConsumedMessageList(string topic) => _messageService.ConsumedMessageList(topic);

    public List<ConsumedMessage> ConsumedListByConnectionId(string connectionId) => _messageService.ConsumedListByConnectionId(connectionId);

    public List<ConsumedMessage> ConsumedList(string connectionId, string topic) => _messageService.ConsumedList(connectionId, topic);
}

