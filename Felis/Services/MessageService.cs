﻿using Felis.Entities;
using Felis.Models;
using LiteDB;

namespace Felis.Services;

internal sealed class MessageService : IDisposable
{
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<MessageEntity> _messageCollection;
    private readonly object _lock = new();

    public MessageService(ILiteDatabase database)
    {
        ArgumentNullException.ThrowIfNull(database);
        _database = database;
        _messageCollection = _database.GetCollection<MessageEntity>("messages");
    }

    public Guid Add(MessageRequestModel message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Payload);

        lock (_lock)
        {
            var messageId = Guid.NewGuid();
            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            _messageCollection.Insert(messageId, new MessageEntity()
            {
                Id = messageId,
                Message = new MessageModel(messageId, message.Topic, message.Payload),
                Timestamp = timestamp
            });

            return messageId;
        }
    }

    public bool Send(Guid messageId)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException(nameof(messageId));
        }

        lock (_lock)
        {
            var messageFound = _messageCollection.FindById(messageId) ?? throw new InvalidOperationException(
                    $"Message {messageId} not found. The send will not be set.");
            
            if (messageFound.Sent.HasValue)
            {
                throw new InvalidOperationException(
                    $"Message {messageId} already sent at {messageFound.Sent.Value}. The send will not be set.");
            }

            messageFound.Sent = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            var updateResult = _messageCollection.Update(messageFound.Id, messageFound);

            return updateResult;
        }
    }

    public List<MessageModel> GetPendingMessagesToByTopic(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        lock (_lock)
        {
            return _messageCollection.Find(x => x.Message.Topic == topic && x.Sent == null).OrderBy(x => x.Timestamp).Select(x => x.Message).ToList();
        }
    }

    public void Dispose()
    {
        _database.Dispose();
    }
}