using LiteDB;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Timer = System.Timers.Timer;

namespace Felis;

internal sealed class MessageBroker : IDisposable
{
    private readonly ConcurrentDictionary<string, List<Subscription>> _topicSubscriptions = new();
    private readonly ILogger<MessageBroker> _logger;
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<MessageModel> _messageCollection;
    private readonly object _lock = new();
    private readonly Timer _timer;

    public MessageBroker(ILogger<MessageBroker> logger, ILiteDatabase database)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(database);
        _logger = logger;
        _database = database;
        _messageCollection = _database.GetCollection<MessageModel>("messages");
        _messageCollection.EnsureIndex(x => x.Timestamp);
        _messageCollection.EnsureIndex(x => x.Topic);
        _timer = new Timer();
        _timer.Interval = 7000;
        _timer.Elapsed += OnTimedEvent!;
        _timer.AutoReset = true;
        _timer.Enabled = true;
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

        lock (_lock)
        {
            var subscription = new Subscription(ipAddress, hostname, topic);

            _logger.LogDebug(
                "Subscribed '{hostname}' with IP: '{ipAddress}' to topic '{topic}' at {timestamp}",
                subscription.Subscriber.Hostname, subscription.Subscriber.IpAddress, subscription.Subscriber.Topic,
                subscription.Subscriber.Timestamp);

            var subscribers = _topicSubscriptions.GetOrAdd(topic.Trim(), _ => new List<Subscription>());

            subscribers.Add(subscription);

            var messages = GetPendingMessagesToByTopic(topic);
            // Send pending messages when a subscriber connects
            foreach (var message in messages)
            {
                var writtenMessage = subscription.MessageChannel.Writer.TryWrite(message);
                _logger.LogDebug(
                    "Written message '{id}': {operationResult} for subscriber '{hostname}' with IP '{ipAddress}' for topic '{topic}'",
                    message.Id, writtenMessage, subscription.Subscriber.Hostname, subscription.Subscriber.IpAddress,
                    subscription.Subscriber.Topic);
            }

            return subscription;
        }
    }

    /// <summary>
    /// Removes a subscriber by id
    /// </summary>
    /// <param name="id"></param>
    public void UnSubscribe(Guid id)
    {
        foreach (var topic in _topicSubscriptions.Keys)
        {
            lock (_lock)
            {
                var subscribers = _topicSubscriptions[topic];
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
    /// <param name="retryAttempts"></param>
    /// <param name="ttl"></param>
    /// <returns>Message id</returns>
    public Guid Publish(string topic, string payload, int? retryAttempts, int? ttl)
    {
        lock (_lock)
        {
            var message = AddMessageInStorage(topic, payload, retryAttempts, ttl);

            if (!_topicSubscriptions.TryGetValue(topic, out var subscribers)) return message.Id;
            if (subscribers.Count <= 0) return message.Id;

            foreach (var subscriber in subscribers)
            {
                var writtenMessage = subscriber.MessageChannel.Writer.TryWrite(message);
                _logger.LogDebug("Written message '{id}': {operationResult}", message.Id, writtenMessage);
            }

            return message.Id;
        }
    }

    /// <summary>
    /// Sets a message in sent status
    /// </summary>
    /// <param name="messageId"></param>
    /// <param name="subscriber"></param>
    /// <returns>If message has been set as sent or not</returns>
    public void Send(Guid messageId, SubscriberModel subscriber)
    {
        lock (_lock)
        {
            if (messageId == Guid.Empty)
            {
                _logger.LogWarning("messageId is an empty Guid. No processing will be done");
                return;
            }

            var messageFound = _messageCollection.FindById(messageId);

            if (messageFound == null)
            {
                _logger.LogWarning("Message '{messageId}' not found. The send will not be set.", messageId);
                return;
            }

            var sentModel = messageFound.Tracking.FirstOrDefault(e =>
                e.Subscriber.IpAddress == subscriber.IpAddress && e.Subscriber.Hostname == subscriber.Hostname &&
                e.Subscriber.Topic == subscriber.Topic);

            if (sentModel != null)
            {
                _logger.LogWarning("Message '{id}' already sent at {timestamp}. The send will not be set.", messageId,
                    sentModel.Sent);
                return;
            }

            messageFound.Tracking.Add(new TrackingModel(subscriber,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()));

            var updateResult = _messageCollection.Update(messageId, messageFound);

            _logger.LogDebug("Message '{id}' sent: {operationResult}", messageId, updateResult);
        }
    }

    /// <summary>
    /// Sets ack date to a message for a subscriber
    /// </summary>
    /// <param name="messageId"></param>
    /// <param name="ipAddress"></param>
    /// <param name="hostname"></param>
    /// <returns>If message has been set as sent or not</returns>
    public void Ack(Guid messageId, string ipAddress, string hostname)
    {
        lock (_lock)
        {
            if (messageId == Guid.Empty)
            {
                _logger.LogWarning("messageId is an empty Guid. No processing will be done");
                return;
            }

            var messageFound = _messageCollection.FindById(messageId);

            if (messageFound == null)
            {
                _logger.LogWarning("Message '{messageId}' not found. The ack will not be set.", messageId);
                return;
            }

            var sentModel = messageFound.Tracking.FirstOrDefault(e =>
                e.Subscriber.IpAddress == ipAddress && e.Subscriber.Hostname == hostname &&
                e.Subscriber.Topic == messageFound.Topic && e.Ack == null && e.Rejected == null);

            if (sentModel == null)
            {
                _logger.LogWarning("Message '{id}' not found. The ack will not be set.", messageId);
                return;
            }

            sentModel.Ack = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            var updateResult = _messageCollection.Update(messageFound);

            _logger.LogDebug("Message '{id}' sent: {operationResult}", messageId, updateResult);
        }
    }

    private MessageModel AddMessageInStorage(string topic, string payload, int? retryAttempts, int? ttl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        lock (_lock)
        {
            var message = new MessageModel(Guid.NewGuid(), topic, payload,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds());

            if (ttl.HasValue)
            {
                message.Expiration = new DateTimeOffset(DateTime.UtcNow.AddSeconds(ttl.Value)).ToUnixTimeMilliseconds();
            }
            
            if (retryAttempts.HasValue)
            {
                message.RetryAttempts = retryAttempts.Value;
            }

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
            var currentTimeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            
            return _messageCollection.Find(x => x.Topic == topic && x.Tracking.Count == 0 && x.Payload != null && (x.Expiration == null || x.Expiration.Value > currentTimeStamp))
                .OrderBy(x => x.Timestamp).ToList();
        }
    }

    private void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
    {
        lock (_lock)
        {
            //find messages with subscribers to be enqueued again
            var currentTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            var candidatesMessagesToUpdate = _messageCollection.Query().Where(x => (x.Expiration == null || x.Expiration.Value > currentTimestamp) && x.RetryAttempts > 0 && x.Tracking.Count(t => t.Ack == null 
                    && t.Rejected == null 
                    && t.Retries.Count <= x.RetryAttempts) > 0)
                .OrderBy(m => m.Timestamp).ToList();

            var messagesToUpdate = new List<MessageModel>();
            
            if (candidatesMessagesToUpdate != null && candidatesMessagesToUpdate.Any())
            {
                _database.BeginTrans();
                try
                {
                    foreach (var candidateMessageToUpdate in candidatesMessagesToUpdate)
                    {
                        //find messages with subscribers to rejected because reached the retry limit
                        candidateMessageToUpdate.Tracking.Where(trackingModel =>
                                _topicSubscriptions.Values.Any(s => s.Any(ts =>
                                    ts.Subscriber.Hash == trackingModel.Subscriber.Hash) && trackingModel.Ack == null &&
                                    trackingModel.Rejected == null &&
                                    trackingModel.Retries.Count == candidateMessageToUpdate.RetryAttempts)).ToList()
                            .ForEach(
                                r =>
                                {
                                    r.Rejected = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

                                    if (messagesToUpdate.All(x => x.Id != candidateMessageToUpdate.Id))
                                    {
                                        messagesToUpdate.Add(candidateMessageToUpdate);
                                    }
                                });


                        candidateMessageToUpdate.Tracking.Where(trackingModel =>
                                _topicSubscriptions.Values.Any(s => s.Any(ts => ts.Subscriber.Hash == trackingModel.Subscriber.Hash)
                                    && trackingModel.Ack == null
                                    && trackingModel.Rejected == null
                                    && trackingModel.Retries.Count < candidateMessageToUpdate.RetryAttempts - 1
                                    && currentTimestamp - trackingModel.Sent > 60)).ToList()
                            .ForEach(
                                r =>
                                {
                                    r.Retries.Add(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds());
                                    //schedule message on the channel for subscriber
                                    _topicSubscriptions[candidateMessageToUpdate.Topic].ForEach(subscription =>
                                    {
                                        if (subscription.Subscriber.Hash == r.Subscriber.Hash)
                                        {
                                            subscription.MessageChannel.Writer.TryWrite(candidateMessageToUpdate);
                                            _logger.LogDebug(
                                                "Enqueued for retry message '{messageId}' for subscription '{subscriptionId}' for subscriber with IP'{ipAddress}' for topic '{topic}'",
                                                candidateMessageToUpdate.Id, subscription.Id, subscription.Subscriber.IpAddress,
                                                candidateMessageToUpdate.Topic);
                                            
                                            if (messagesToUpdate.All(x => x.Id != candidateMessageToUpdate.Id))
                                            {
                                                messagesToUpdate.Add(candidateMessageToUpdate);
                                            }
                                        }
                                    });
                                });
                    }

                    if (messagesToUpdate.Count > 0)
                    {
                        var updatedMessageToUpdateResult = _messageCollection.Update(messagesToUpdate);
                        _logger.LogDebug("Updated messages {operationResult}", updatedMessageToUpdateResult);
                    }
                    else
                    {
                        _logger.LogDebug("No messages to update");
                    }

                    _database.Commit();
                }
                catch (Exception ex)
                {
                    _logger.LogError("An error '{error}' has occurred during retry check", ex.Message);
                    _database.Rollback();
                }
            }
        }
    }

    public void Dispose()
    {
        _database.Dispose();
    }
}

internal record MessageModel(Guid Id, string Topic, string? Payload, long Timestamp)
{
    public long? Expiration { get; set; }
    [JsonIgnore] public int RetryAttempts { get; set; }
    [JsonIgnore] public List<TrackingModel> Tracking { get; set; } = new();
};

internal record SubscriberModel(string Hostname, string IpAddress, string Topic, long Timestamp)
{
    public string Hash => GetHash();

    private string GetHash()
    {
        var md5Hasher = MD5.Create();
        var data = md5Hasher.ComputeHash(Encoding.Default.GetBytes($"{Hostname}/{IpAddress}/{Topic}"));
        var stringBuilder = new StringBuilder();
        for (var i = 0; i < data.Length; i++)
        {
            stringBuilder.Append(data[i].ToString("x2"));
        }
        return stringBuilder.ToString();
    }
};

internal record TrackingModel(SubscriberModel Subscriber, long Sent)
{
    public long? Ack { get; set; }
    public long? Rejected { get; set; }
    public List<long> Retries { get; set; } = new();
}

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