using LiteDB;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Felis;

public sealed class MessageBroker : IDisposable
{
    private readonly ILogger<MessageBroker> _logger;
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<MessageModel> _messageCollection;
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<string, List<SubscriptionModel>> _subscriptions = new();
    private readonly ConcurrentDictionary<string, int> _topicIndex = new();

    private delegate void NotifyMessagePublish(object sender, MessageModel message);
    private delegate void NotifySubscribeByTopic(object sender, string topic);
    private event NotifyMessagePublish? NotifyPublish;
    private event NotifySubscribeByTopic? NotifySubscribe;

    internal MessageBroker(ILogger<MessageBroker> logger, ILiteDatabase database)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(database);
        _logger = logger;
        _database = database;
        _messageCollection = _database.GetCollection<MessageModel>("messages");
        _messageCollection.EnsureIndex(x => x.Timestamp);
        _messageCollection.EnsureIndex(x => x.Topic);
        NotifyPublish += OnMessagePublished;
        NotifySubscribe += OnMessageSubscribed;
    }

    /// <summary>
    /// Publishes a message to a topic
    /// </summary>
    /// <param name="topic">The destination topic</param>
    /// <param name="payload">The message payload</param>
    /// <returns>Message id</returns>
    public Guid Publish(string topic, string payload)
    {
        lock (_lock)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);
            ArgumentException.ThrowIfNullOrWhiteSpace(payload);

            var topicTrimmedLowered = topic.Trim().ToLowerInvariant();

            var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

            var message = new MessageModel(Guid.NewGuid(), topicTrimmedLowered, payload, timestamp);

            NotifyPublish?.Invoke(this, message);

            return message.Id;
        }
    }
    
    /// <summary>
    /// Subscribes and listens to incoming messages on a topic
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="exclusive"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IAsyncEnumerable<MessageModel?> Subscribe(string topic, bool exclusive, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var subscription = new SubscriptionModel(Guid.NewGuid(),
            new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), exclusive);

        var topicTrimmedLowered = topic.Trim().ToLowerInvariant();

        lock (_lock)
        {
            var subscriptions = _subscriptions.GetOrAdd(topicTrimmedLowered, _ => new List<SubscriptionModel>());

            subscriptions.Add(subscription);

            if (!_topicIndex.ContainsKey(topicTrimmedLowered))
            {
                _topicIndex.TryAdd(topicTrimmedLowered, 0);
            }
        }

        _logger.LogInformation(
            "Subscribed '{id}' to topic '{topic}' at {timestamp}", subscription.Id, topicTrimmedLowered,
            subscription.Timestamp);

        cancellationToken.Register(() => { UnSubscribe(topicTrimmedLowered, subscription); });

        NotifySubscribe?.Invoke(this, topicTrimmedLowered);

        return subscription.GetNextAvailableMessageAsync(cancellationToken);
    }

    public void Dispose()
    {
        _database.Dispose();
        NotifyPublish -= OnMessagePublished;
        NotifySubscribe -= OnMessageSubscribed;
    }

    private async void OnMessagePublished(object source, MessageModel message)
    {
        try
        {
            var enqueueResult = await EnqueueMessage(message);
            _logger.LogDebug("Message '{messageId}' enqueue result: {enqueueResult}", message.Id, enqueueResult);
        }
        catch (Exception ex)
        {
            _logger.LogError("An error '{error}' has occurred during publish", ex.Message);
        }
    }
    
    private async void OnMessageSubscribed(object source, string topic)
    {
        try
        {
            var nextMessages = _messageCollection
                .Query()
                .Where(x =>
                    x.Topic == topic
                )
                .OrderBy(x => x.Timestamp)
                .ToList();

            if (nextMessages.Count == 0) return;

            foreach (var nextMessage in nextMessages)
            {
                var enqueueResult = await EnqueueMessage(nextMessage);
                _logger.LogDebug("Message '{messageId}' enqueue result: {enqueueResult}", nextMessage.Id, enqueueResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("An error '{error}' has occurred during subscribe", ex.Message);
        }
    }

    private async Task<bool> EnqueueMessage(MessageModel message)
    {
        var subscription = GetNextSubscription(message.Topic);

        if (subscription != null)
        {
            await subscription.WriteMessageAsync(message, CancellationToken.None);
            _logger.LogInformation(
                "Message '{messageId}' sent to '{subscriptionId}' at {timestamp} for topic '{topic}'",
                message.Id, subscription.Id,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), message.Topic);

            var deleteResult = DeleteMessage(message);
            _logger.LogDebug("Message '{id}' deleted: {operationResult}", message.Id, deleteResult);
            return true;
        }

        MessageStore(message);
        return false;
    }

    private void MessageStore(MessageModel message)
    {
        lock (_lock)
        {
            _messageCollection.Insert(message.Id, message);
            _logger.LogDebug("Added message '{id}' for topic '{topic}' in storage with payload '{payload}'",
                message.Id, message.Topic, message.Payload);
        }
    }
   
    private bool DeleteMessage(MessageModel nextMessage)
    {
        var deleteResult = _messageCollection.Delete(nextMessage.Id);
        return deleteResult;
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

            var exclusiveSubscription = subscriptions.FirstOrDefault(x => x.Exclusive);

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

public record MessageModel(Guid Id, string Topic, string? Payload, long Timestamp);

record SubscriptionModel(Guid Id, long Timestamp, bool Exclusive)
{
    private readonly Channel<MessageModel> _messageChannel = Channel.CreateBounded<MessageModel>(1);

    internal async ValueTask WriteMessageAsync(MessageModel messageModel, CancellationToken cancellationToken) =>
        await _messageChannel.Writer.WriteAsync(messageModel, cancellationToken);

    internal IAsyncEnumerable<MessageModel?> GetNextAvailableMessageAsync(CancellationToken cancellationToken) =>
        _messageChannel.Reader.ReadAllAsync(cancellationToken);
}