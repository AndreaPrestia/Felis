using Felis.Entities;
using Felis.Models;
using LiteDB;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Felis;

internal sealed class MessageBroker : IDisposable
{
    private readonly ConcurrentDictionary<string, List<SubscriberEntity>> _topicSubscribers = new();
    private readonly ILogger<MessageBroker> _logger;
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<MessageEntity> _messageCollection;
    private readonly object _lock = new();

    public MessageBroker(ILogger<MessageBroker> logger, ILiteDatabase database)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(database);
        _logger = logger;
        _database = database;
        _messageCollection = _database.GetCollection<MessageEntity>("messages");
    }

    /// <summary>
    /// Add a subscriber to a topic
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <param name="hostname"></param>
    /// <param name="topics"></param>
    /// <returns></returns>
    public SubscriberEntity Subscribe(string ipAddress, string hostname, List<string> topics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        ArgumentNullException.ThrowIfNull(topics);

        var subscriberEntity = new SubscriberEntity(ipAddress, hostname, topics);

        foreach (var topic in topics)
        {
            var subscribers = _topicSubscribers.GetOrAdd(topic.Trim(), _ => new List<SubscriberEntity>());
            lock (subscribers)
            {
                subscribers.Add(subscriberEntity);
            }

            var messages = GetPendingMessagesToByTopic(topic);
            // Send pending messages when a subscriber connects
            foreach (var message in messages)
            {
                var writtenMessage = subscriberEntity.MessageChannel.Writer.TryWrite(message);
                _logger.LogDebug($"Written message {message.Id}: {writtenMessage}");
            }
        }

        return subscriberEntity;
    }

    /// <summary>
    /// Removes a subscriber by id
    /// </summary>
    /// <param name="id"></param>
    public void UnSubscribe(Guid id)
    {
        foreach (var topic in _topicSubscribers.Keys)
        {
            var subscribers = _topicSubscribers[topic];
            lock (subscribers)
            {
                subscribers.RemoveAll(s => s.Id == id);
            }
        }
    }

    /// <summary>
    /// Publish a message to a topic
    /// </summary>
    /// <param name="message"></param>
    /// <returns>Message id</returns>
    public Guid Publish(MessageRequestModel message)
    {
        var messageId = AddMessageInStorage(message);

        if (!_topicSubscribers.TryGetValue(message.Topic, out var subscribers)) return messageId;
        if (subscribers.Count <= 0) return messageId;
        
        foreach (var subscriber in subscribers)
        {
            var writtenMessage = subscriber.MessageChannel.Writer.TryWrite(new MessageModel(messageId, message.Topic, message.Payload));
            _logger.LogDebug($"Written message {messageId}: {writtenMessage}");
        }

        return messageId;
    }

    /// <summary>
    /// Gets a list of subscribers by topic
    /// </summary>
    /// <param name="topic"></param>
    /// <returns></returns>
    public List<SubscriberModel> Subscribers(string topic)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);

            return _topicSubscribers.Where(x => x.Key.Contains(topic)).SelectMany(e => e.Value)
                .Select(r => new SubscriberModel(r.Hostname, r.IpAddress, r.Topics)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<SubscriberModel>();
        }
    }

    /// <summary>
    /// Sets a message in sent status
    /// </summary>
    /// <param name="messageId"></param>
    /// <returns>If message has been set as sent or not</returns>
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

    private Guid AddMessageInStorage(MessageRequestModel message)
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

    private List<MessageModel> GetPendingMessagesToByTopic(string topic)
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