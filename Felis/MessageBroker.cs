using LiteDB;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Timer = System.Timers.Timer;

namespace Felis;

internal sealed class MessageBroker : IDisposable
{
    private readonly ILogger<MessageBroker> _logger;
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<MessageModel> _messageCollection;
    private readonly object _lock = new();
    private readonly Timer _topicTimer;
    private readonly Timer _heartbeatTimer;
    private readonly ConcurrentDictionary<string, List<SubscriptionModel>> _subscriptions = new();
    private readonly ConcurrentDictionary<string, int> _topicIndex = new();

    public MessageBroker(ILogger<MessageBroker> logger, ILiteDatabase database, int heartbeatInSeconds)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(database);
        _logger = logger;
        _database = database;
        _messageCollection = _database.GetCollection<MessageModel>("messages");
        _messageCollection.EnsureIndex(x => x.Timestamp);
        _messageCollection.EnsureIndex(x => x.Topic);
        _topicTimer = new Timer
        {
            Interval = 1000
        };
        _topicTimer.Elapsed += OnTopicTimedEvent!;
        _topicTimer.AutoReset = true;
        _topicTimer.Enabled = true;
        _heartbeatTimer = new Timer
        {
            Interval = heartbeatInSeconds * 1000
        };
        _heartbeatTimer.Elapsed += OnHeartbeatTimedEvent!;
        _heartbeatTimer.AutoReset = true;
        _heartbeatTimer.Enabled = true;
    }

    /// <summary>
    /// Add a subscriber to a topic
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <param name="hostname"></param>
    /// <param name="topic"></param>
    /// <param name="exclusive"></param>
    /// <returns></returns>
    internal SubscriptionModel Subscribe(string topic, string ipAddress, string hostname, bool? exclusive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);

        lock (_lock)
        {
            var topicTrimmedLowered = topic.Trim().ToLowerInvariant();

            var subscription = new SubscriptionModel(Guid.NewGuid(), hostname, ipAddress,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), exclusive);

            var subscriptions = _subscriptions.GetOrAdd(topicTrimmedLowered, _ => new List<SubscriptionModel>());

            subscriptions.Add(subscription);

            if (!_topicIndex.ContainsKey(topicTrimmedLowered))
            {
                _topicIndex.TryAdd(topicTrimmedLowered, 0);
            }

            _logger.LogInformation(
                "Subscribed '{id}' with hostname: '{hostname}' with IP: '{ipAddress}' to topic '{topic}' at {timestamp}",
                subscription.Id, subscription.Hostname, subscription.IpAddress, topicTrimmedLowered,
                subscription.Timestamp);

            return subscription;
        }
    }

    /// <summary>
    /// Removes a subscriber from a topic
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="subscription"></param>
    internal void UnSubscribe(string topic, SubscriptionModel subscription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(subscription);

        lock (_lock)
        {
            var topicTrimmedLowered = topic.Trim().ToLowerInvariant();

            _logger.LogDebug("Remove subscription from topic '{topic}'", topicTrimmedLowered);
            var unSubscribeResult = _subscriptions[topicTrimmedLowered].Remove(subscription);
            _logger.LogInformation(
                "UnSubscribed '{id}' with hostname: '{hostname}' with IP: '{ipAddress}' to topic '{topic}' at {timestamp} with operation result '{operationResult}'",
                subscription.Id, subscription.Hostname, subscription.IpAddress, topicTrimmedLowered,
                subscription.Timestamp, unSubscribeResult);

            if (_topicIndex.TryGetValue(topicTrimmedLowered, out var currentIndex))
            {
                currentIndex = (currentIndex + 1) % _subscriptions[topicTrimmedLowered].Count;
                _topicIndex[topicTrimmedLowered] = currentIndex;
            }
        }
    }

    /// <summary>
    /// Publish a message to a topic
    /// </summary>
    /// <param name="topic">The destination topic</param>
    /// <param name="payload">The message payload</param>
    /// <param name="ttl">The message TTL</param>
    /// <param name="broadcast">Tells if the message must be broad-casted or sent to only one subscriber in a round robin manner</param>
    /// <returns>Message id</returns>
    internal Guid Publish(string topic, string payload, int? ttl, bool? broadcast)
    {
        lock (_lock)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentException.ThrowIfNullOrWhiteSpace(payload);

            lock (_lock)
            {
                var topicTrimmedLowered = topic.Trim().ToLowerInvariant();

                var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

                var message = new MessageModel(Guid.NewGuid(), topicTrimmedLowered, payload,
                    timestamp,
                    ttl is > 0
                        ? new DateTimeOffset(DateTime.UtcNow.AddSeconds(ttl.Value)).ToUnixTimeMilliseconds()
                        : null, broadcast.HasValue && broadcast.Value);

                _messageCollection.Insert(message.Id, message);

                _logger.LogDebug("Added message '{id}' for topic '{topic}' in storage with payload '{payload}'",
                    message.Id, message.Topic, message.Payload);

                return message.Id;
            }
        }
    }

    /// <summary>
    /// Resets a queue
    /// </summary>
    /// <param name="topic"></param>
    /// <returns></returns>
    internal int Reset(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        lock (_lock)
        {
            var topicTrimmedLowered = topic.Trim().ToLowerInvariant();

            var deletedResult = _messageCollection.DeleteMany(x => x.Topic == topicTrimmedLowered);

            _logger.LogDebug("Deletes messages for topic '{topic}': {operationResult}", topic, deletedResult);

            return deletedResult;
        }
    }

    /// <summary>
    /// Returns all the messages to process in the queue order by timestamp
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="page"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    internal List<MessageModel> Messages(string topic, int page, int size)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        lock (_lock)
        {
            var topicTrimmedLowered = topic.Trim().ToLowerInvariant();

            if (page <= 0)
            {
                page = 1;
            }

            if (size <= 0 || size > 100)
            {
                size = 100;
            }

            var skip = (page - 1) * size;

            var messages = _messageCollection
                .Find(x =>
                    x.Topic == topicTrimmedLowered)
                .OrderBy(x => x.Timestamp)
                .Skip(skip)
                .Take(size)
                .ToList();

            return messages;
        }
    }

    public void Dispose()
    {
        _database.Dispose();
        _topicTimer.Dispose();
        _heartbeatTimer.Dispose();
    }

    private async void OnTopicTimedEvent(object source, System.Timers.ElapsedEventArgs e)
    {
        var currentTimeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

        await Parallel.ForEachAsync(_subscriptions.Keys, async (topic, token) =>
        {
            try
            {
                var nextMessages = _messageCollection.Query().Where(x => x.Topic == topic
                                                                         && (x.Expiration == null ||
                                                                             x.Expiration.Value > currentTimeStamp))
                    .OrderBy(x => x.Timestamp).ToList();

                if (nextMessages.Count == 0) return;

                foreach (var nextMessage in nextMessages)
                {
                    if (nextMessage.Broadcast)
                    {
                        var subscriptions = _subscriptions[topic];

                        if (subscriptions.Count <= 0) return;
                        var tasks = subscriptions
                            .Select(async s =>
                            {
                                await s.MessageChannel.Writer.WriteAsync(nextMessage, token);
                                _logger.LogInformation(
                                    "Message '{messageId}' sent to '{ipAddress}' - '{hostname}' at {timestamp} for topic '{topic}'",
                                    nextMessage.Id, s.IpAddress, s.Hostname,
                                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), nextMessage.Topic);
                            });

                        await Task.WhenAll(tasks);
                    }
                    else
                    {
                        var subscription = GetNextSubscription(topic);

                        if (subscription != null)
                        {
                            await subscription.MessageChannel.Writer.WriteAsync(nextMessage, token);
                            _logger.LogInformation(
                                "Message '{messageId}' sent to '{ipAddress}' - '{hostname}' at {timestamp} for topic '{topic}'",
                                nextMessage.Id, subscription.IpAddress, subscription.Hostname,
                                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), nextMessage.Topic);
                        }
                    }

                    var deleteResult = _messageCollection.Delete(nextMessage.Id);
                    _logger.LogDebug("Message '{id}' deleted: {operationResult}", nextMessage.Id, deleteResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("An error '{error}' has occurred during publish", ex.Message);
            }
        });
    }

    private async void OnHeartbeatTimedEvent(object source, System.Timers.ElapsedEventArgs e)
    {
        var currentTimeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

        await Parallel.ForEachAsync(_subscriptions, async (subscription, token) =>
        {
            try
            {
                if (subscription.Value.Count <= 0) return;

                var tasks = subscription
                    .Value.Select(async s =>
                    {
                        var hearBeatMessage = new MessageModel(Guid.NewGuid(), subscription.Key, $"Heartbeat: {currentTimeStamp}",
                            new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), null, true);

                        await s.MessageChannel.Writer.WriteAsync(hearBeatMessage, token);
                        _logger.LogInformation(
                            "Heartbeat Message '{messageId}' sent to '{ipAddress}' - '{hostname}' at {timestamp} for topic '{topic}'",
                            hearBeatMessage.Id, s.IpAddress, s.Hostname,
                            new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), hearBeatMessage.Topic);
                    });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error '{error}' has occurred during publish", ex.Message);
            }
        });
    }

    private SubscriptionModel? GetNextSubscription(string topic)
    {
        lock (_lock)
        {
            if (_subscriptions.IsEmpty)
            {
                _logger.LogWarning("No subscriptions available.");
                return null;
            }

            if (!_subscriptions.TryGetValue(topic, out List<SubscriptionModel>? subscriptions))
            {
                _logger.LogWarning("No subscriptions available for topic '{topic}'.", topic);
                return null;
            }

            if (subscriptions == null! || subscriptions.Count == 0)
            {
                _logger.LogWarning("No subscriptions for topic '{topic}'. No processing will be done.", topic);
                return null;
            }

            var exclusiveSubscription = subscriptions.FirstOrDefault(x => x.Exclusive.HasValue && x.Exclusive.Value);

            if (exclusiveSubscription != null)
            {
                _logger.LogDebug("Found exclusive subscription '{subscriptionId}' for topic '{topic}'",
                    exclusiveSubscription.Id, topic);

                return exclusiveSubscription;
            }

            if (!_topicIndex.TryGetValue(topic, out var currentIndex))
            {
                currentIndex = 0;
                _topicIndex[topic] = currentIndex;
            }

            var subscription = subscriptions.ElementAt(currentIndex);

            if (subscription == null!)
            {
                _logger.LogWarning("No subscription found for topic '{topic}' at index {currentIndex}", topic,
                    currentIndex);
                return null;
            }

            _logger.LogDebug("Found subscription for topic '{topic}' for index {currentIndex}", topic, currentIndex);

            _topicIndex[topic] = (currentIndex + 1) % subscriptions.Count;

            _logger.LogDebug("New calculated index for next iteration for topic '{topic}' {currentIndex}", topic,
                _topicIndex[topic]);

            return subscription;
        }
    }
}

internal record MessageModel(Guid Id, string Topic, string? Payload, long Timestamp, long? Expiration, bool Broadcast);

internal record SubscriptionModel(Guid Id, string Hostname, string IpAddress, long Timestamp, bool? Exclusive)
{
    public Channel<MessageModel> MessageChannel { get; set; } = Channel.CreateBounded<MessageModel>(1);
}