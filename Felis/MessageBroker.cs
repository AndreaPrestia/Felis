using LiteDB;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;
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
    private readonly ConcurrentDictionary<string, List<SubscriptionModel>> _subscriptions = new();
    private readonly ConcurrentDictionary<string, int> _topicIndex = new();

    public MessageBroker(ILogger<MessageBroker> logger, ILiteDatabase database)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(database);
        _logger = logger;
        _database = database;
        _messageCollection = _database.GetCollection<MessageModel>("messages");
        _messageCollection.EnsureIndex(x => x.Timestamp);
        _messageCollection.EnsureIndex(x => x.Topic);
        _topicTimer = new Timer();
        _topicTimer.Interval = 5000;
        _topicTimer.Elapsed += OnTopicTimedEvent!;
        _topicTimer.AutoReset = true;
        _topicTimer.Enabled = true;
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

            if (_topicIndex.TryGetValue(topicTrimmedLowered, out int currentIndex))
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
                    timestamp)
                {
                    Broadcast = broadcast.HasValue && broadcast.Value
                };

                if (ttl.HasValue && ttl.Value > 0)
                {
                    message.Expiration =
                        new DateTimeOffset(DateTime.UtcNow.AddSeconds(ttl.Value)).ToUnixTimeMilliseconds();
                }

                _messageCollection.Insert(message.Id, message);

                _logger.LogDebug("Added message '{id}' for topic '{topic}' in storage with payload '{payload}'", message.Id, message.Topic, message.Payload);

                return message.Id;
            }
        }
    }

    public void Dispose()
    {
        _database.Dispose();
        _topicTimer.Dispose();
    }

    private async void OnTopicTimedEvent(object source, System.Timers.ElapsedEventArgs e)
    {
        var currentTimeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

        await Parallel.ForEachAsync(_subscriptions.Keys, async (topic, token) =>
            {
                try
                {
                    var nextMessages = _messageCollection.Query().Where(x => x.Topic == topic && x.Tracking.Count == 0
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
                                    nextMessage.Tracking.Add(new TrackingModel(
                                        new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), s.Hostname,
                                        s.IpAddress));
                                });

                            await Task.WhenAll(tasks);
                        }
                        else
                        {
                            var subscription = GetNextSubscription(topic);

                            if (subscription != null)
                            {
                                await subscription.MessageChannel.Writer.WriteAsync(nextMessage, token);
                                nextMessage.Tracking.Add(new TrackingModel(
                                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), subscription.Hostname,
                                    subscription.IpAddress));
                                _logger.LogInformation("Sent message '{messageId}' to subscription '{subscriptionId}'",
                                    nextMessage.Id, subscription.Id);
                            }
                        }

                        var updateResult = _messageCollection.Update(nextMessage.Id, nextMessage);
                        _logger.LogDebug("Message '{id}' sent: {operationResult}", nextMessage.Id, updateResult);
                    }
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

            if (!_subscriptions.ContainsKey(topic))
            {
                _logger.LogWarning("No subscriptions available for topic '{topic}'.", topic);
                return null;
            }

            if (_subscriptions[topic].Count == 0)
            {
                _logger.LogWarning("No subscriptions for topic '{topic}'. No processing will be done.", topic);
                return null;
            }

            var exclusiveSubscription = _subscriptions[topic].FirstOrDefault(x => x.Exclusive.HasValue && x.Exclusive.Value);

            if (exclusiveSubscription != null)
            {
                _logger.LogDebug("Found exclusive subscription '{subscriptionId}' for topic '{topic}'",
                    exclusiveSubscription.Id, topic);

                return exclusiveSubscription;
            }

            if (!_topicIndex.ContainsKey(topic))
            {
                _topicIndex[topic] = 0;
            }

            var subscription = _subscriptions[topic].ElementAt(_topicIndex[topic]);

            if (subscription == null!)
            {
                _logger.LogWarning("No subscription found for topic '{topic}' at index {currentIndex}", topic,
                    _topicIndex[topic]);
                return null;
            }

            _logger.LogDebug("Found subscription for topic '{topic}' for index {currentIndex}", topic, _topicIndex[topic]);

            _topicIndex[topic] = (_topicIndex[topic] + 1) % _subscriptions[topic].Count;

            _logger.LogDebug("New calculated index for next iteration for topic '{topic}' {currentIndex}", topic,
                _topicIndex[topic]);

            return subscription;
        }
    }
}

internal record MessageModel(Guid Id, string Topic, string? Payload, long Timestamp)
{
    public long? Expiration { get; set; }
    [JsonIgnore] public bool Broadcast { get; init; }
    [JsonIgnore] public List<TrackingModel> Tracking { get; set; } = new();
};

internal record TrackingModel(long Timestamp, string? Hostname, string? IpAddress);

internal record SubscriptionModel(Guid Id, string Hostname, string IpAddress, long Timestamp, bool? Exclusive)
{
    public Channel<MessageModel> MessageChannel { get; set; } = Channel.CreateUnbounded<MessageModel>();
}