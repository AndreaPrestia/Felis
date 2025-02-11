﻿using LiteDB;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Timer = System.Timers.Timer;

namespace Felis;

public sealed class MessageBroker : IDisposable
{
    private readonly ILogger<MessageBroker> _logger;
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<MessageModel> _messageCollection;
    private readonly object _lock = new();
    private readonly Timer _topicTimer;
    private readonly Timer _heartbeatTimer;
    private readonly ConcurrentDictionary<string, List<SubscriptionModel>> _subscriptions = new();
    private readonly ConcurrentDictionary<string, int> _topicIndex = new();

    internal MessageBroker(ILogger<MessageBroker> logger, ILiteDatabase database)
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
            Interval = 5 * 1000
        };
        _heartbeatTimer.Elapsed += OnHeartbeatTimedEvent!;
        _heartbeatTimer.AutoReset = true;
        _heartbeatTimer.Enabled = true;
    }
    
    /// <summary>
    /// Publishes a message to a topic
    /// </summary>
    /// <param name="topic">The destination topic</param>
    /// <param name="payload">The message payload</param>
    /// <param name="ttl">The message TTL</param>
    /// <param name="broadcast">Tells if the message must be broad-casted or sent to only one subscriber in a round robin manner</param>
    /// <returns>Message id</returns>
    public Guid Publish(string topic, string payload, int? ttl, bool? broadcast)
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
    /// Subscribes and listens to incoming messages on a topic
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="exclusive"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IAsyncEnumerable<MessageModel?> Subscribe(string topic, bool? exclusive, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        
        var subscription = new SubscriptionModel(Guid.NewGuid(),
            new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), exclusive);

        lock (_lock)
        {
            var topicTrimmedLowered = topic.Trim().ToLowerInvariant();

            var subscriptions = _subscriptions.GetOrAdd(topicTrimmedLowered, _ => new List<SubscriptionModel>());

            subscriptions.Add(subscription);

            if (!_topicIndex.ContainsKey(topicTrimmedLowered))
            {
                _topicIndex.TryAdd(topicTrimmedLowered, 0);
            }

            _logger.LogInformation(
                "Subscribed '{id}' to topic '{topic}' at {timestamp}", subscription.Id, topicTrimmedLowered,
                subscription.Timestamp);
        }

        cancellationToken.Register(() =>
        {
            UnSubscribe(topic, subscription);
        });

        return subscription.GetNextAvailableMessageAsync(cancellationToken);
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
                                await s.WriteMessageAsync(nextMessage, token);
                                _logger.LogInformation(
                                    "Message '{messageId}' sent to '{subscriptionId}' at {timestamp} for topic '{topic}'",
                                    nextMessage.Id, s.Id,
                                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), nextMessage.Topic);
                            });

                        await Task.WhenAll(tasks);
                    }
                    else
                    {
                        var subscription = GetNextSubscription(topic);

                        if (subscription != null)
                        {
                            await subscription.WriteMessageAsync(nextMessage, token);
                            _logger.LogInformation(
                                "Message '{messageId}' sent to '{subscriptionId}' at {timestamp} for topic '{topic}'",
                                nextMessage.Id, subscription.Id,
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
                        var hearBeatMessage = new MessageModel(Guid.NewGuid(), subscription.Key,
                            $"Heartbeat: {currentTimeStamp}",
                            new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), null, true);

                        await s.WriteMessageAsync(hearBeatMessage, token);
                        _logger.LogInformation(
                            "Heartbeat Message '{messageId}' sent to '{subscriptionId}' at {timestamp} for topic '{topic}'",
                            hearBeatMessage.Id, s.Id, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(),
                            hearBeatMessage.Topic);
                    });

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error '{error}' has occurred during publish", ex.Message);
            }
        });
    }
    
    private void UnSubscribe(string topic, SubscriptionModel subscription)
    {
        lock (_lock)
        {
            var topicTrimmedLowered = topic.Trim().ToLowerInvariant();

            _logger.LogDebug("Remove subscription from topic '{topic}'", topicTrimmedLowered);
            var unSubscribeResult = _subscriptions[topicTrimmedLowered].Remove(subscription);
            _logger.LogInformation(
                "UnSubscribed '{id}' from topic '{topic}' at {timestamp} with operation result '{operationResult}'",
                subscription.Id, topicTrimmedLowered, subscription.Timestamp, unSubscribeResult);

            if (_topicIndex.TryGetValue(topicTrimmedLowered, out var currentIndex))
            {
                currentIndex = (currentIndex + 1) % _subscriptions[topicTrimmedLowered].Count;
                _topicIndex[topicTrimmedLowered] = currentIndex;
            }
        }
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

public record MessageModel(Guid Id, string Topic, string? Payload, long Timestamp, long? Expiration, bool Broadcast);

record SubscriptionModel(Guid Id, long Timestamp, bool? Exclusive)
{
    private readonly Channel<MessageModel> _messageChannel = Channel.CreateBounded<MessageModel>(1);

    internal async ValueTask WriteMessageAsync(MessageModel messageModel, CancellationToken cancellationToken) =>
        await _messageChannel.Writer.WriteAsync(messageModel, cancellationToken);

    internal IAsyncEnumerable<MessageModel?> GetNextAvailableMessageAsync(CancellationToken cancellationToken) =>
        _messageChannel.Reader.ReadAllAsync(cancellationToken);
}