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
    private readonly ConcurrentDictionary<string, List<SubscriptionModel>> _subscriptions = new();
    private readonly ConcurrentDictionary<string, int> _topicIndex = new();

    private delegate void NotifyMessagePublish(object sender, MessageModel message);

    private delegate void NotifySubscribeByTopic(object sender, string queue);

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
        _messageCollection.EnsureIndex(x => x.Queue);
        NotifyPublish += OnMessagePublished;
        NotifySubscribe += OnMessageSubscribed;
    }

    /// <summary>
    /// Publishes a message to a queue
    /// </summary>
    /// <param name="queue">The destination queue</param>
    /// <param name="payload">The message payload</param>
    /// <returns>Message id</returns>
    public MessageModel Publish(string queue, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var normalizedName = queue.Trim().ToLowerInvariant();

        var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

        var message = new MessageModel(Guid.NewGuid(), normalizedName, payload, timestamp);

        NotifyPublish?.Invoke(this, message);

        return message;
    }

    /// <summary>
    /// Subscribes and listens to incoming messages on a queue
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="exclusive"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IAsyncEnumerable<MessageModel?> Subscribe(string queue, bool exclusive, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);

        var subscription = new SubscriptionModel(Guid.NewGuid(),
            new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), exclusive);

        var topicTrimmedLowered = queue.Trim().ToLowerInvariant();

        var subscriptions = _subscriptions.GetOrAdd(topicTrimmedLowered, _ => new List<SubscriptionModel>());

        subscriptions.Add(subscription);

        if (!_topicIndex.ContainsKey(topicTrimmedLowered))
        {
            _topicIndex.TryAdd(topicTrimmedLowered, 0);
        }

        _logger.LogInformation(
            "Subscribed '{id}' to queue '{queue}' at {timestamp}", subscription.Id, topicTrimmedLowered,
            subscription.Timestamp);

        cancellationToken.Register(() =>
        {
            var unSubscribeResult = _subscriptions[topicTrimmedLowered].Remove(subscription);
            _logger.LogInformation(
                "UnSubscribed '{id}' from queue '{queue}' at {timestamp} with operation result '{operationResult}'",
                subscription.Id, topicTrimmedLowered, subscription.Timestamp, unSubscribeResult);

            if (_topicIndex.TryGetValue(topicTrimmedLowered, out var currentIndex))
            {
                currentIndex = (currentIndex + 1) % _subscriptions[topicTrimmedLowered].Count;
                _topicIndex[topicTrimmedLowered] = currentIndex;
            }
        });

        NotifySubscribe?.Invoke(this, topicTrimmedLowered);

        return subscription.GetNextAvailableMessageAsync(cancellationToken);
    }

    /// <summary>
    /// Reset the content of a queue
    /// </summary>
    /// <param name="queue"></param>
    /// <returns></returns>
    public int Reset(string queue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);

        _database.BeginTrans();
        
        try
        {
            var normalizedTopic = queue.ToLower().Trim();
            var deletedItems = _messageCollection.DeleteMany(x => x.Queue == normalizedTopic);
            _database.Commit();
            return deletedItems;
        }
        catch (Exception)
        {
            _database.Rollback();
            throw;
        }
    }

    public void Dispose()
    {
        _database.Dispose();
        NotifyPublish -= OnMessagePublished;
        NotifySubscribe -= OnMessageSubscribed;
    }

    private void OnMessagePublished(object source, MessageModel message)
    {
        try
        {
            _messageCollection.Insert(message.Id, message);
            _logger.LogDebug("Added message '{id}' for queue '{queue}' in storage with payload '{payload}'",
                message.Id, message.Queue, message.Payload);
            NotifySubscribe?.Invoke(this, message.Queue);
        }
        catch (Exception ex)
        {
            _logger.LogError("An error '{error}' has occurred during publish", ex.Message);
        }
    }

    private async void OnMessageSubscribed(object source, string queue)
    {
        try
        {
            _database.BeginTrans();
            
            var message = _messageCollection
                .Query()
                .Where(x =>
                    x.Queue == queue
                )
                .OrderBy(x => x.Timestamp)
                .FirstOrDefault();

            if (message == null)
            {
                _database.Rollback();
                return;
            }

            var subscription = GetNextSubscription(message.Queue);

            if (subscription != null)
            {
                await subscription.WriteMessageAsync(message, CancellationToken.None);
                _logger.LogInformation(
                    "Message '{messageId}' sent to '{subscriptionId}' at {timestamp} for queue '{queue}'",
                    message.Id, subscription.Id,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), message.Queue);

                var deleteResult = _messageCollection.Delete(message.Id);
                _logger.LogDebug("Message '{id}' deleted: {operationResult}", message.Id, deleteResult);
                _database.Commit();
                return;
            }

            _database.Rollback();
        }
        catch (Exception ex)
        {
            _logger.LogError("An error '{error}' has occurred during subscribe", ex.Message);
        }
    }

    private SubscriptionModel? GetNextSubscription(string queue)
    {
        if (_subscriptions.IsEmpty)
        {
            _logger.LogWarning("No subscriptions available.");
            return null;
        }

        if (!_subscriptions.TryGetValue(queue, out List<SubscriptionModel>? subscriptions))
        {
            _logger.LogWarning("No subscriptions available for queue '{queue}'.", queue);
            return null;
        }

        if (subscriptions == null! || subscriptions.Count == 0)
        {
            _logger.LogWarning("No subscriptions for queue '{queue}'. No processing will be done.", queue);
            return null;
        }

        var exclusiveSubscription = subscriptions.FirstOrDefault(x => x.Exclusive);

        if (exclusiveSubscription != null)
        {
            _logger.LogDebug("Found exclusive subscription '{subscriptionId}' for queue '{queue}'",
                exclusiveSubscription.Id, queue);
            return exclusiveSubscription;
        }

        if (!_topicIndex.TryGetValue(queue, out var currentIndex))
        {
            currentIndex = 0;
            _topicIndex[queue] = currentIndex;
        }

        var subscription = subscriptions.ElementAt(currentIndex);

        if (subscription == null!)
        {
            _logger.LogWarning("No subscription found for queue '{queue}' at index {currentIndex}", queue,
                currentIndex);
            return null;
        }

        _logger.LogDebug("Found subscription for queue '{queue}' for index {currentIndex}", queue, currentIndex);

        _topicIndex[queue] = (currentIndex + 1) % subscriptions.Count;

        _logger.LogDebug("New calculated index for next iteration for queue '{queue}' {currentIndex}", queue,
            _topicIndex[queue]);

        return subscription;
    }
}

public record MessageModel(Guid Id, string Queue, string? Payload, long Timestamp);

record SubscriptionModel(Guid Id, long Timestamp, bool Exclusive)
{
    private readonly Channel<MessageModel> _messageChannel = Channel.CreateBounded<MessageModel>(1);

    internal async ValueTask WriteMessageAsync(MessageModel messageModel, CancellationToken cancellationToken) =>
        await _messageChannel.Writer.WriteAsync(messageModel, cancellationToken);

    internal IAsyncEnumerable<MessageModel?> GetNextAvailableMessageAsync(CancellationToken cancellationToken) =>
        _messageChannel.Reader.ReadAllAsync(cancellationToken);
}