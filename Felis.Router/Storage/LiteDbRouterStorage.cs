﻿using Felis.Core.Models;
using Felis.Router.Abstractions;
using Felis.Router.Entities;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Storage;

internal sealed class LiteDbRouterStorage : IRouterStorage
{
    private readonly ILiteDatabase _database;
    private readonly ILogger<LiteDbRouterStorage> _logger;
    private readonly object _lock = new();

    public LiteDbRouterStorage(ILiteDatabase database, ILogger<LiteDbRouterStorage> logger)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(logger);
        _database = database;
        _logger = logger;
    }

    public bool ConsumedMessageAdd(ConsumedMessage? consumedMessage)
    {
        ArgumentNullException.ThrowIfNull(consumedMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumedMessage.ConnectionId);

        lock (_lock)
        {
            if (consumedMessage.Id == Guid.Empty)
            {
                throw new ArgumentException(nameof(consumedMessage.Id));
            }

            var messageCollection = _database.GetCollection<MessageEntity>("messages");

            var messageFound = messageCollection.FindById(consumedMessage.Id);

            if (messageFound == null)
            {
                _logger.LogWarning($"Message {consumedMessage.Id} not found");
                return false;
            }

            messageFound.Status = MessageStatus.Consumed;

            messageFound.Ack.Add(new MessageAcknowledgement()
            {
                MessageId = consumedMessage.Id,
                ConnectionId = consumedMessage.ConnectionId,
                Timestamp = consumedMessage.Timestamp
            });

            var updateResult = messageCollection.Update(consumedMessage.Id, messageFound);
            messageFound.UpdatedAt = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            _logger.LogDebug($"Update result {updateResult}");

            return updateResult;
        }
    }

    public bool ProcessedMessageAdd(ProcessedMessage? processedMessage)
    {
        ArgumentNullException.ThrowIfNull(processedMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(processedMessage.ConnectionId);

        lock (_lock)
        {
            if (processedMessage.Id == Guid.Empty)
            {
                throw new ArgumentException(nameof(processedMessage.Id));
            }

            var messageCollection = _database.GetCollection<MessageEntity>("messages");

            var messageFound = messageCollection.FindById(processedMessage.Id);

            if (messageFound == null)
            {
                _logger.LogWarning($"Message {processedMessage.Id} not found");
                return false;
            }

            messageFound.Status = MessageStatus.Processed;
            messageFound.UpdatedAt = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            var updateResult = messageCollection.Update(processedMessage.Id, messageFound);

            _logger.LogDebug($"Update result {updateResult}");

            return updateResult;
        }
    }

    public bool ReadyMessageAdd(Message? message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.Header);
        ArgumentNullException.ThrowIfNull(message.Header.Topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Header.Topic);
        ArgumentNullException.ThrowIfNull(message.Content);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Content.Payload);

        lock (_lock)
        {
            var messageCollection = _database.GetCollection<MessageEntity>("messages");

            messageCollection.Insert(message.Header.Id, new MessageEntity()
            {
                Id = message.Header.Id,
                Timestamp = message.Header.Timestamp,
                Topic = message.Header.Topic,
                Payload = message.Content.Payload,
                UpdatedAt = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(),
                Status = MessageStatus.Ready
            });

            return true;
        }
    }

    public Message? ReadyMessageGet()
    {
        lock (_lock)
        {
            var messageCollection = _database.GetCollection<MessageEntity>("messages");

            var messageFound = messageCollection.Query().Where(x => x.Status == MessageStatus.Ready).OrderBy(e => e.Timestamp).FirstOrDefault();

            if (messageFound == null)
            {
                _logger.LogWarning("No Queued message found to send");
                return null;
            }

            return new Message(new Header(messageFound.Id, messageFound.Topic, messageFound.Timestamp), new Content(messageFound.Payload));
        }
    }

    public List<Message> ReadyMessageList(string? topic = null)
    {
        lock (_lock)
        {
            var messageCollection = _database.GetCollection<MessageEntity>("messages");

            var messages = !string.IsNullOrWhiteSpace(topic) ? messageCollection.Query().Where(x => x.Topic == topic && x.Status == MessageStatus.Ready).OrderBy(x => x.Timestamp).ToList() : messageCollection.Query().Where(x => x.Status == MessageStatus.Ready).OrderBy(x => x.Timestamp).ToList();

            return messages != null
                ? messages.Select(m =>
                    new Message(new Header(m.Id, m.Topic, m.Timestamp), new Content(m.Payload))).ToList()
                : new();
        }
    }

    public bool SentMessageAdd(Message? message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(message.Header);
        ArgumentNullException.ThrowIfNull(message.Header.Topic);

        lock (_lock)
        {
            var messageCollection = _database.GetCollection<MessageEntity>("messages");

            var messageFound = messageCollection.FindById(message.Header.Id);

            if (messageFound == null)
            {
                _logger.LogWarning($"No message {message.Header.Id} with status {MessageStatus.Ready} not found. The send will not be set.");
                return false;
            }

            if (messageFound.Status != MessageStatus.Ready)
            {
                _logger.LogWarning($"Message {message.Header.Id} with status {messageFound.Status} found. The send will not be set.");
                return false;
            }

            messageFound.Status = MessageStatus.Sent;
            messageFound.UpdatedAt = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            var updateResult = messageCollection.Update(messageFound.Id, messageFound);

            _logger.LogDebug($"Update result {updateResult}");

            return true;
        }
    }

    public List<Message> SentMessageList(string? topic = null)
    {
        lock (_lock)
        {
            var messageCollection = _database.GetCollection<MessageEntity>("messages");

            var messages = !string.IsNullOrWhiteSpace(topic) ? messageCollection.Query().Where(x => x.Topic == topic && x.Status == MessageStatus.Sent).OrderBy(x => x.Timestamp).ToList() : messageCollection.Query().Where(x => x.Status == MessageStatus.Sent).OrderBy(x => x.Timestamp).ToList();

            return messages != null
                ? messages.Select(m =>
                    new Message(new Header(m.Id, m.Topic, m.Timestamp), new Content(m.Payload))).ToList()
                : new();
        }
    }

    public List<ConsumedMessage> ConsumedMessageListByConnectionId(string connectionId)
    {
        lock (_lock)
        {
            var messageCollection = _database.GetCollection<MessageEntity>("messages");

            var messages = messageCollection.Query().Where(x => x.Ack.Any()).OrderBy(x => x.Timestamp).ToList();

            return messages != null
                ? messages.SelectMany(x => x.Ack).Select(ack => new ConsumedMessage(ack.MessageId, ack.ConnectionId, ack.Timestamp)).ToList()
                : new();

        }
    }

    public List<ConsumedMessage> ConsumedMessageList(string topic)
    {
        lock (_lock)
        {
            var messageCollection = _database.GetCollection<MessageEntity>("messages");

            var messages = messageCollection.Query().Where(x => x.Ack.Any() && x.Topic == topic).OrderBy(x => x.Timestamp).ToList();

            return messages != null
                ? messages.SelectMany(x => x.Ack).Select(ack => new ConsumedMessage(ack.MessageId, ack.ConnectionId, ack.Timestamp)).ToList()
                : new();
        }
    }

    public List<ConsumedMessage> ConsumedMessageList(string connectionId, string topic)
    {
        lock (_lock)
        {
            var messageCollection = _database.GetCollection<MessageEntity>("messages");

            var messages = messageCollection.Query().Where(x => x.Ack.Any(ack => ack.ConnectionId == connectionId) && x.Topic == topic).OrderBy(x => x.Timestamp).ToList();

            return messages != null
                ? messages.SelectMany(x => x.Ack).Select(ack => new ConsumedMessage(ack.MessageId, ack.ConnectionId, ack.Timestamp)).ToList()
                : new();
        }
    }

    public bool ReadyMessagePurge(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        lock (_lock)
        {
            var messageCollection = _database.GetCollection<MessageEntity>("messages");

            var deleteResult = messageCollection.DeleteMany(x => x.Status == MessageStatus.Ready && x.Topic == topic);

            return deleteResult > 0;
        }
    }

    public bool ErrorMessageAdd(ErrorMessageRequest? message)
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
                return false;
            }

            var messageCollection = _database.GetCollection<MessageEntity>("messages");

            var messageFound = messageCollection.FindById(message.Id);

            if (messageFound == null)
            {
                _logger.LogWarning($"Message {message.Id} not found. No error processing will be done.");
                return false;
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

            var updateResult = messageCollection.Update(messageFound.Id, messageFound);

            _logger.LogDebug($"Update result {updateResult}");

            return updateResult;
        }
    }

    public List<ErrorMessage> ErrorMessageList(string? topic = null)
    {
        lock (_lock)
        {
            var messageCollection = _database.GetCollection<MessageEntity>("messages");

            var messages = topic != null && !string.IsNullOrWhiteSpace(topic)
                ? messageCollection.Query().Where(x => x.Status == MessageStatus.Error && x.Topic == topic).ToList()
                : messageCollection.Query().Where(x => x.Status == MessageStatus.Error).ToList();

            return messages.Select(m => new ErrorMessage(m.Id, new Message(new Header(m.Id, m.Topic, m.Timestamp), new Content(m.Payload)), m.Errors.Select(d => new ErrorMessageDetail(d.ConnectionId, d.Details.Select(dt => new ErrorDetail(dt.Title, dt.Detail)).ToList(), d.RetryPolicy != null ? new RetryPolicy(d.RetryPolicy.Attempts) : null)).ToList())).ToList();
        }
    }
}