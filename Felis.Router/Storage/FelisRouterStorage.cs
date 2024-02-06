using System.Collections.Concurrent;
using Felis.Core;
using Felis.Core.Models;

namespace Felis.Router.Storage;

/// <summary>
/// This is an in-memory storage implementation of FelisStorage.
/// </summary>
public sealed class FelisRouterStorage
{
    private ConcurrentQueue<Message?> _messages = new();
    private ConcurrentQueue<ConsumedMessage?> _consumedMessages = new();
    private readonly ConcurrentDictionary<ErrorMessage, int> _errorMessages = new();

    public bool ConsumedMessageAdd(ConsumedMessage? consumedMessage)
    {
        if (_consumedMessages.Any(cm => cm?.ConnectionId.Value == consumedMessage?.ConnectionId.Value
                                        && string.Equals(cm?.Message?.Header?.Topic?.Value,
                                            consumedMessage?.Message?.Header?.Topic?.Value,
                                            StringComparison.InvariantCultureIgnoreCase)
                                        && cm?.Timestamp == consumedMessage?.Timestamp))
        {
            return false;
        }

        _consumedMessages = new ConcurrentQueue<ConsumedMessage?>(_consumedMessages.Append(consumedMessage));

        _messages = new ConcurrentQueue<Message?>(_messages.Where(x =>
            x?.Id != consumedMessage?.Message?.Id));

        return true;
    }

    public bool MessageAdd(Message? message)
    {
        _messages = new ConcurrentQueue<Message?>(_messages.Append(message));

        return true;
    }

    public List<Message?> MessageList(Topic? topic = null)
    {
        return _messages.Where(m =>
                topic == null || string.IsNullOrWhiteSpace(topic.Value) ||
                m!.Header!.Topic!.Value!.Contains(topic.Value))
            .ToList();
    }

    public List<ConsumedMessage?> ConsumedMessageList(ConnectionId connectionId)
    {
        return _consumedMessages.Where(cm => cm?.ConnectionId.Value == connectionId.Value)
            .ToList();
    }

    public List<ConsumedMessage?> ConsumedMessageList(Topic topic)
    {
        return _consumedMessages
            .Where(cm =>
                string.Equals(cm?.Message?.Header?.Topic?.Value, topic.Value,
                    StringComparison.InvariantCultureIgnoreCase))
            .ToList();
    }
    
    public List<ConsumedMessage?> ConsumedMessageList(ConnectionId connectionId, Topic topic)
    {
        return _consumedMessages.Where(cm => cm?.ConnectionId.Value == connectionId.Value && string.Equals(cm?.Message?.Header?.Topic?.Value, topic.Value, StringComparison.InvariantCultureIgnoreCase))
            .ToList();
    }

    public List<ErrorMessage> ListMessagesToRequeue()
    {
        return _errorMessages.Where(em => em.Key.RetryPolicy?.Attempts >= em.Value).Select(em => em.Key).ToList();
    }

    public bool MessagePurge(Topic? topic)
    {
        _messages = new ConcurrentQueue<Message?>(_messages.Where(m =>
                !string.Equals(topic?.Value, m?.Header?.Topic?.Value, StringComparison.InvariantCultureIgnoreCase))
            .OrderBy(m => m?.Header?.Timestamp));

        return true;
    }

    public bool MessagePurge(int? timeToLiveMinutes)
    {
        if (!timeToLiveMinutes.HasValue || timeToLiveMinutes <= 0)
        {
            return false;
        }

        _messages = new ConcurrentQueue<Message?>(_messages
            .Where(m => m?.Header?.Timestamp < new DateTimeOffset(DateTime.UtcNow).AddMinutes(-timeToLiveMinutes.Value)
                .ToUnixTimeMilliseconds())
            .OrderBy(m => m?.Header?.Timestamp));

        return true;
    }

    public bool ErrorMessageAdd(ErrorMessage message)
    {
        var errorMessageFound = _errorMessages.FirstOrDefault(em =>
            em.Key.Message?.Id == message.Message?.Id && em.Key.ConnectionId?.Value == message.ConnectionId?.Value);

        if (errorMessageFound.Equals(default(KeyValuePair<ErrorMessage, int>)))
        {
            return _errorMessages.TryAdd(message, message.RetryPolicy == null ? 0 : 1);
        }

        if (message.RetryPolicy == null)
        {
            return _errorMessages.TryUpdate(errorMessageFound.Key, 0, errorMessageFound.Value);
        }
        
        return _errorMessages.TryUpdate(errorMessageFound.Key, errorMessageFound.Value + 1, errorMessageFound.Value);
    }

    public List<ErrorMessage> ErrorMessageList(Topic? topic = null)
    {
        return _errorMessages.Select(em => em.Key)
            .Where(em => topic == null || string.Equals(em.Message?.Header?.Topic?.Value, topic.Value, StringComparison.InvariantCultureIgnoreCase)).ToList();
    }
}