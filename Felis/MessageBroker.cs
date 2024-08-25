using LiteDB;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Threading.Channels;

namespace Felis;

internal sealed class MessageBroker : IDisposable
{
    private readonly ConcurrentDictionary<string, List<Subscription>> _topicSubscriptions = new();
    private readonly ILogger<MessageBroker> _logger;
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<MessageModel> _messageCollection;
    private readonly object _lock = new();

    public MessageBroker(ILogger<MessageBroker> logger, ILiteDatabase database)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(database);
        _logger = logger;
        _database = database;
        _messageCollection = _database.GetCollection<MessageModel>("messages");
    }

    /// <summary>
    /// Add a subscriber to a topic
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <param name="hostname"></param>
    /// <param name="topic"></param>
    /// <returns></returns>
    public Subscription Subscribe(string ipAddress, string hostname, string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var subscription = new Subscription(ipAddress, hostname, topic);

        _logger.LogDebug(
            "Subscribed '{hostname}' with IP: '{ipAddress}' to topic '{topic}' at {timestamp}",
            subscription.Subscriber.Hostname, subscription.Subscriber.IpAddress, subscription.Subscriber.Topic, subscription.Subscriber.Timestamp);

        var subscribers = _topicSubscriptions.GetOrAdd(topic.Trim(), _ => new List<Subscription>());
        lock (_lock)
        {
            subscribers.Add(subscription);

            var messages = GetPendingMessagesToByTopic(topic);
            // Send pending messages when a subscriber connects
            foreach (var message in messages)
            {
                var writtenMessage = subscription.MessageChannel.Writer.TryWrite(message);
                _logger.LogDebug(
                    "Written message '{id}': {operationResult} for subscriber '{hostname}' with IP '{ipAddress}' for topic '{topic}'",
                    message.Id, writtenMessage, subscription.Subscriber.Hostname, subscription.Subscriber.IpAddress, subscription.Subscriber.Topic);
            }
        }

        return subscription;
    }

    /// <summary>
    /// Removes a subscriber by id
    /// </summary>
    /// <param name="id"></param>
    public void UnSubscribe(Guid id)
    {
        foreach (var topic in _topicSubscriptions.Keys)
        {
            var subscribers = _topicSubscriptions[topic];
            lock (_lock)
            {
                var unSubscribeResult = subscribers.RemoveAll(s => s.Id == id);
                _logger.LogInformation("UnSubscribed {id}: {operationResult}", id, unSubscribeResult);
            }
        }
    }

    /// <summary>
    /// Publish a message to a topic
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="payload"></param>
    /// <returns>Message id</returns>
    public Guid Publish(string topic, string payload)
    {
        var message = AddMessageInStorage(topic, payload);

        if (!_topicSubscriptions.TryGetValue(topic, out var subscribers)) return message.Id;
        if (subscribers.Count <= 0) return message.Id;

        foreach (var subscriber in subscribers)
        {
            var writtenMessage = subscriber.MessageChannel.Writer.TryWrite(message);
            _logger.LogDebug("Written message '{id}': {operationResult}", message.Id, writtenMessage);
        }

        return message.Id;
    }

    /// <summary>
    /// Sets a message in sent status
    /// </summary>
    /// <param name="messageId"></param>
    /// <param name="subscriber"></param>
    /// <returns>If message has been set as sent or not</returns>
    public void Send(Guid messageId, SubscriberModel subscriber)
    {
        if (messageId == Guid.Empty)
        {
            _logger.LogWarning("messageId is an empty Guid. No processing will be done");
            return;
        }

        lock (_lock)
        {
            var messageFound = _messageCollection.FindById(messageId) ?? throw new InvalidOperationException(
                $"Message {messageId} not found. The send will not be set.");

            var sentModel = messageFound.Send.FirstOrDefault(e =>
                e.Subscriber.IpAddress == subscriber.IpAddress && e.Subscriber.Hostname == subscriber.Hostname &&
                e.Subscriber.Topic == subscriber.Topic);

            if (sentModel != null)
            {
                _logger.LogWarning("Message '{id}' already sent at {timestamp}. The send will not be set.", messageId, sentModel.Sent);
            }

            messageFound.Send.Add(new SendModel(subscriber,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()));

            var updateResult = _messageCollection.Update(messageFound.Id, messageFound);

            _logger.LogDebug("Message '{id}' sent: {operationResult}", messageId, updateResult);
        }
    }

    private MessageModel AddMessageInStorage(string topic, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        lock (_lock)
        {
            var message = new MessageModel(Guid.NewGuid(), topic, payload,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds());

            _messageCollection.Insert(message.Id, message);
            
            _logger.LogDebug("Added message '{id}' in storage", message.Id);

            return message;
        }
    }

    private List<MessageModel> GetPendingMessagesToByTopic(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        lock (_lock)
        {
            return _messageCollection.Find(x => x.Topic == topic && x.Send.Count == 0 && x.Payload != null)
                .OrderBy(x => x.Timestamp).ToList();
        }
    }

    public void Dispose()
    {
        _database.Dispose();
    }
}

internal record MessageModel(Guid Id, string Topic, string? Payload, long Timestamp)
{
    [JsonIgnore] public List<SendModel> Send { get; } = new();
};

internal record SubscriberModel(string Hostname, string IpAddress, string Topic, long Timestamp);

internal record SendModel(SubscriberModel Subscriber, long Sent);

internal record Subscription
{
    public Guid Id { get; }
    public Channel<MessageModel> MessageChannel { get; }
    public SubscriberModel Subscriber { get; }

    public Subscription(string ipAddress, string hostname, string topic)
    {
        Id = Guid.NewGuid();
        MessageChannel = Channel.CreateUnbounded<MessageModel>();
        Subscriber = new SubscriberModel(hostname, ipAddress, topic,
            new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds());
    }
}