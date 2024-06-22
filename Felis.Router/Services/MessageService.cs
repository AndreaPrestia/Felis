using Felis.Router.Entities;
using Felis.Router.Enums;
using Felis.Router.Models;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Services;

internal sealed class MessageService : IDisposable
{
    private readonly ILiteDatabase _database;
    private readonly ILogger<MessageService> _logger;
    private readonly ILiteCollection<MessageEntity> _messageCollection;
    private readonly object _lock = new();

    public MessageService(ILiteDatabase database, ILogger<MessageService> logger)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(logger);
        _database = database;
        _logger = logger;
        _messageCollection = _database.GetCollection<MessageEntity>("messages");
        _messageCollection.EnsureIndex(x => x.EnqueuedAt);
    }

    public MessageStatus Add(MessageRequest? message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Payload);

        lock (_lock)
        {
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            _messageCollection.Insert(message.Id, new MessageEntity()
            {
                Id = message.Id,
                Timestamp = timestamp,
                Topic = message.Topic,
                Payload = message.Payload,
                UpdatedAt = timestamp,
                Status = MessageStatus.Ready
            });

            return MessageStatus.Ready;
        }
    }

    public MessageStatus Consume(ConsumedMessage? consumedMessage)
    {
        ArgumentNullException.ThrowIfNull(consumedMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumedMessage.ConnectionId);

        lock (_lock)
        {
            if (consumedMessage.Id == Guid.Empty)
            {
                throw new ArgumentException(nameof(consumedMessage.Id));
            }

            var messageFound = _messageCollection.FindById(consumedMessage.Id);

            if (messageFound == null)
            {
                _logger.LogWarning($"Message {consumedMessage.Id} not found");
                return MessageStatus.Error;
            }

            messageFound.Ack.Add(new MessageAcknowledgement()
            {
                MessageId = consumedMessage.Id,
                ConnectionId = consumedMessage.ConnectionId,
                Timestamp = consumedMessage.Timestamp
            });

            if (messageFound.Status != MessageStatus.Consumed)
            {
                messageFound.Status = MessageStatus.Consumed;
            }
            
            messageFound.UpdatedAt = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            var updateResult = _messageCollection.Update(consumedMessage.Id, messageFound);

            _logger.LogDebug($"Update result {updateResult}");

            return messageFound.Status;
        }
    }

    public MessageStatus Error(ErrorMessageRequest? message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.ConnectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.ConnectionId);
        ArgumentNullException.ThrowIfNull(message.Error);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Error.Detail);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Error.Title);

        lock (_lock)
        {
            if (message.Id == Guid.Empty)
            {
                return MessageStatus.Error;
            }

            var messageFound = _messageCollection.FindById(message.Id);

            if (messageFound == null)
            {
                _logger.LogWarning($"Message {message.Id} not found. No error processing will be done.");
                return MessageStatus.Error;
            }

            var hasRetryPolicy = message.RetryPolicy is { Attempts: > 0 };

            var error = messageFound.Errors.FirstOrDefault(x => x.ConnectionId == message.ConnectionId);

            if (error == null)
            {
                error = new MessageError()
                {
                    ConnectionId = message.ConnectionId
                };

                messageFound.Errors.Add(error);
            }

            error.Details.Add(new()
            {
                Title = message.Error.Title,
                Detail = message.Error.Detail,
                Timestamp = message.Timestamp
            });

            error.RetryPolicy = hasRetryPolicy
            ? new MessageRetryPolicy()
            {
                Attempts = message.RetryPolicy!.Attempts
            }
            : null;

            if (hasRetryPolicy)
            {
                messageFound.Retries.Add(new MessageRetry()
                {
                    ConnectionId = message.ConnectionId,
                    Timestamp = message.Timestamp
                });

                var messageRetries = messageFound.Errors.All(x => x.RetryPolicy == null) ? new List<MessageRetry>() : messageFound.Retries.ToList();

                var firstRetryToApply = messageRetries.Where(x => x.Sent == null).MinBy(x => x.Timestamp);

                if (messageRetries.Count == 0 || firstRetryToApply == null)
                {
                    messageFound.Status = MessageStatus.Error;
                }
                else
                {
                    messageFound.Status = MessageStatus.Ready;
                }
            }
            else
            {
                messageFound.Status = MessageStatus.Error;
            }

            messageFound.UpdatedAt = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            var updateResult = _messageCollection.Update(messageFound.Id, messageFound);

            _logger.LogDebug($"Update result {updateResult}");

            return messageFound.Status;
        }
    }

    public MessageStatus Process(ProcessedMessage? processedMessage)
    {
        ArgumentNullException.ThrowIfNull(processedMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(processedMessage.ConnectionId);

        lock (_lock)
        {
            if (processedMessage.Id == Guid.Empty)
            {
                throw new ArgumentException(nameof(processedMessage.Id));
            }

            var messageFound = _messageCollection.FindById(processedMessage.Id);

            if (messageFound == null)
            {
                _logger.LogWarning($"Message {processedMessage.Id} not found");
                return MessageStatus.Error;
            }

            messageFound.Status = MessageStatus.Processed;
            messageFound.UpdatedAt = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            var updateResult = _messageCollection.Update(processedMessage.Id, messageFound);

            _logger.LogDebug($"Update result {updateResult}");

            return messageFound.Status;
        }
    }

    public Message? Get(Guid messageId)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException(nameof(messageId));
        }

        lock (_lock)
        {
            var messageFound = _messageCollection.FindById(messageId);

            if (messageFound == null)
            {
                _logger.LogWarning($"No message found with id {messageId}");
                return null;
            }

            return new Message(new Header(Guid.Parse(messageFound.Id.ToString()), messageFound.Topic, messageFound.Timestamp), new Content(messageFound.Payload), messageFound.Status.ToString());
        }
    }

    public MessageStatus Send(Guid messageId)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException(nameof(messageId));
        }

        lock (_lock)
        {
            var messageFound = _messageCollection.FindById(messageId);

            if (messageFound == null)
            {
                _logger.LogWarning($"No message {messageId} with status {MessageStatus.Ready} not found. The send will not be set.");
                return MessageStatus.Error;
            }

            if (messageFound.Status != MessageStatus.Ready)
            {
                _logger.LogWarning($"Message {messageId} with status {messageFound.Status} found. The send will not be set.");
                return MessageStatus.Error;
            }

            messageFound.Status = MessageStatus.Sent;
            messageFound.UpdatedAt = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            var updateResult = _messageCollection.Update(messageFound.Id, messageFound);

            _logger.LogDebug($"Update result {updateResult}");

            return messageFound.Status;
        }
    }

    public int Purge(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        lock (_lock)
        {
            var deleteResult = _messageCollection.DeleteMany(x => x.Status == MessageStatus.Ready && x.Topic == topic);

            return deleteResult;
        }
    }

    public List<Message> ReadyList(string? topic = null)
    {
        lock (_lock)
        {
            var messages = !string.IsNullOrWhiteSpace(topic) ? _messageCollection.Query().Where(x => x.Topic == topic && x.Status == MessageStatus.Ready).OrderBy(x => x.Timestamp).ToList() : _messageCollection.Query().Where(x => x.Status == MessageStatus.Ready).OrderBy(x => x.Timestamp).ToList();

            return messages != null
                ? messages.Select(m =>
                    new Message(new Header(Guid.Parse(m.Id.ToString()), m.Topic, m.Timestamp), new Content(m.Payload), m.Status.ToString())).ToList()
                : new();
        }
    }

    public List<Message> SentList(string? topic = null)
    {
        lock (_lock)
        {
            var messages = !string.IsNullOrWhiteSpace(topic) ? _messageCollection.Query().Where(x => x.Topic == topic && x.Status == MessageStatus.Sent).OrderBy(x => x.Timestamp).ToList() : _messageCollection.Query().Where(x => x.Status == MessageStatus.Sent).OrderBy(x => x.Timestamp).ToList();

            return messages != null
                ? messages.Select(m =>
                    new Message(new Header(Guid.Parse(m.Id.ToString()), m.Topic, m.Timestamp), new Content(m.Payload), m.Status.ToString())).ToList()
                : new();
        }
    }

    public List<ErrorMessage> ErrorList(string? topic = null)
    {
        lock (_lock)
        {
            var messages = topic != null && !string.IsNullOrWhiteSpace(topic)
                ? _messageCollection.Query().Where(x => x.Status == MessageStatus.Error && x.Topic == topic).ToList()
                : _messageCollection.Query().Where(x => x.Status == MessageStatus.Error).ToList();

            return messages.Select(m => new ErrorMessage(Guid.Parse(m.Id.ToString()), new Message(new Header(Guid.Parse(m.Id.ToString()), m.Topic, m.Timestamp), new Content(m.Payload), m.Status.ToString()), m.Errors.Select(d => new ErrorMessageDetail(d.ConnectionId, d.Details.Select(dt => new ErrorDetail(dt.Title, dt.Detail)).ToList(), d.RetryPolicy != null ? new RetryPolicy(d.RetryPolicy.Attempts) : null)).ToList())).ToList();
        }
    }

    public List<ConsumedMessage> ConsumedMessageList(string topic)
    {
        lock (_lock)
        {
            var messages = _messageCollection.Query().Where(x => x.Ack.Any() && x.Topic == topic).OrderBy(x => x.Timestamp).ToList();

            return messages != null
                ? messages.SelectMany(x => x.Ack).Select(ack => new ConsumedMessage(ack.MessageId, ack.ConnectionId, ack.Timestamp)).ToList()
                : new();
        }
    }

    public List<ConsumedMessage> ConsumedListByConnectionId(string connectionId)
    {
        lock (_lock)
        {
            var messages = _messageCollection.Query().Where(x => x.Ack.Any(a => a.ConnectionId == connectionId)).OrderBy(x => x.Timestamp).ToList();

            return messages != null
                ? messages.SelectMany(x => x.Ack).Select(ack => new ConsumedMessage(ack.MessageId, ack.ConnectionId, ack.Timestamp)).ToList()
                : new();

        }
    }

    public List<ConsumedMessage> ConsumedList(string connectionId, string topic)
    {
        lock (_lock)
        {
            var messages = _messageCollection.Query().Where(x => x.Ack.Any(ack => ack.ConnectionId == connectionId) && x.Topic == topic).OrderBy(x => x.Timestamp).ToList();

            return messages != null
                ? messages.SelectMany(x => x.Ack).Select(ack => new ConsumedMessage(ack.MessageId, ack.ConnectionId, ack.Timestamp)).ToList()
                : new();
        }
    }

    public void Dispose()
    {
        _database.Dispose();
    }
}