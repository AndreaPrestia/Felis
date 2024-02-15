using System.Collections.Concurrent;
using System.Text.Json;
using Felis.Core.Models;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Storage;

/// <summary>
/// This is an in-memory storage implementation of FelisStorage.
/// </summary>
internal sealed class FelisRouterStorage
{
    private ConcurrentQueue<Message?> _readyMessages = new();
    private ConcurrentQueue<Message?> _sentMessages = new();
    private ConcurrentQueue<ConsumedMessage?> _consumedMessages = new();
    private ConcurrentQueue<ErrorMessage?> _errorMessages = new();
    private ConcurrentDictionary<Guid, int> _errorMessagesWithRetries = new();

    private readonly ILogger<FelisRouterStorage> _logger;

    public FelisRouterStorage(ILogger<FelisRouterStorage> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

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

        _sentMessages = new ConcurrentQueue<Message?>(_sentMessages.Where(x =>
            x?.Header?.Id != consumedMessage?.Message?.Header?.Id));

        return true;
    }

    public bool ReadyMessageAdd(Message? message)
    {
        _readyMessages = new ConcurrentQueue<Message?>(_readyMessages.Append(message));

        return true;
    }

    public Message? ReadyMessageGet()
    {
        var isDequeued = _readyMessages.TryDequeue(out var message);

        if (isDequeued)
            _logger.LogInformation($"Dequeued ready message {message?.Header?.Id}");

        return message;
    }

    public List<Message?> ReadyMessageList(Topic? topic = null)
    {
        return _readyMessages.Where(m =>
                topic == null || string.IsNullOrWhiteSpace(topic.Value) ||
                m!.Header!.Topic!.Value!.Contains(topic.Value))
            .ToList();
    }

    public bool SentMessageAdd(Message? message)
    {
        _sentMessages = new ConcurrentQueue<Message?>(_sentMessages.Append(message));

        return true;
    }

    public List<Message?> SentMessageList(Topic? topic = null)
    {
        return _sentMessages.Where(m =>
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
        return _consumedMessages.Where(cm =>
                cm?.ConnectionId.Value == connectionId.Value && string.Equals(cm?.Message?.Header?.Topic?.Value,
                    topic.Value, StringComparison.InvariantCultureIgnoreCase))
            .ToList();
    }

    public bool ReadyMessagePurge(Topic? topic)
    {
        _readyMessages = new ConcurrentQueue<Message?>(_readyMessages.Where(m =>
                !string.Equals(topic?.Value, m?.Header?.Topic?.Value, StringComparison.InvariantCultureIgnoreCase))
            .OrderBy(m => m?.Header?.Timestamp));

        return true;
    }

    public bool ReadyMessagePurge(int? timeToLiveMinutes)
    {
        if (!timeToLiveMinutes.HasValue || timeToLiveMinutes <= 0)
        {
            return false;
        }

        _readyMessages = new ConcurrentQueue<Message?>(_readyMessages
            .Where(m => m?.Header?.Timestamp < new DateTimeOffset(DateTime.UtcNow).AddMinutes(-timeToLiveMinutes.Value)
                .ToUnixTimeMilliseconds())
            .OrderBy(m => m?.Header?.Timestamp));

        return true;
    }

    public bool ErrorMessageAdd(ErrorMessage? message)
    {
        if (message?.Message?.Header == null)
        {
            return false;
        }

        if (message.RetryPolicy is not { Attempts: > 0 })
        {
            _logger.LogWarning($"Error message without Attempts: {JsonSerializer.Serialize(message)}");
            return true;
        }

        var retryFound = _errorMessagesWithRetries.FirstOrDefault(em =>
            em.Key == message.Message?.Header?.Id);

        if (retryFound.Equals(default(KeyValuePair<Guid, int>)))
        {
            _errorMessages = new ConcurrentQueue<ErrorMessage?>(_errorMessages.Append(message));
            _errorMessagesWithRetries = new ConcurrentDictionary<Guid, int>(
                _errorMessagesWithRetries.Append(new KeyValuePair<Guid, int>(message.Message.Header.Id, 1)));

            return true;
        }

        if (_errorMessages.All(x => x?.Message?.Header?.Id != message.Message.Header.Id))
        {
            _errorMessages = new ConcurrentQueue<ErrorMessage?>(_errorMessages.Append(message));
        }

        return _errorMessagesWithRetries.TryUpdate(message.Message.Header.Id, retryFound.Value + 1, retryFound.Value);
    }

    public ErrorMessage? ErrorMessageGet()
    {
        var isDequeued = _errorMessages.TryDequeue(out var errorMessage);

        if (isDequeued)
            _logger.LogInformation($"Dequeued error message {errorMessage?.Message?.Header?.Id}");

        var retriesDone = _errorMessagesWithRetries.FirstOrDefault(em =>
                em.Key == errorMessage?.Message?.Header?.Id && errorMessage.RetryPolicy?.Attempts >= em.Value).Value;

        if (retriesDone > errorMessage?.RetryPolicy?.Attempts)
        {
            _logger.LogWarning($"Error message finished Attempts: {JsonSerializer.Serialize(errorMessage)}");
            return null;
        }
        
        if(retriesDone <= errorMessage?.RetryPolicy?.Attempts)
        {
            _errorMessages =
                new ConcurrentQueue<ErrorMessage?>(_errorMessages.Append(errorMessage));
            return null;
        }

        return errorMessage;
    }

    public List<ErrorMessage?> ErrorMessageList(Topic? topic = null)
    {
        return _errorMessages
            .Where(em => topic == null || string.Equals(em?.Message?.Header?.Topic?.Value, topic.Value,
                StringComparison.InvariantCultureIgnoreCase)).ToList();
    }
}