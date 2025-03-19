using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;
using MessagePack;
using Tenray.ZoneTree;
using Tenray.ZoneTree.Comparers;
using Tenray.ZoneTree.Serializers;

namespace Felis;

public sealed class MessageBroker : IDisposable
{
    private readonly ILogger<MessageBroker> _logger;
    private readonly IZoneTree<string, Memory<byte>> _zoneTree;
    private readonly IMaintainer _maintainer;
    private readonly ConcurrentDictionary<string, List<Subscription>> _subscriptions = new();
    private readonly ConcurrentDictionary<string, int> _topicIndex = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private delegate void NotifyMessagePublish(object sender, Message message);
    private delegate void NotifySubscribeByTopic(object sender, string queue);
    private event NotifyMessagePublish? NotifyPublish;
    private event NotifySubscribeByTopic? NotifySubscribe;

    internal MessageBroker(string dataPath, ILogger<MessageBroker> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataPath);
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _zoneTree = new ZoneTreeFactory<string, Memory<byte>>()
            .SetComparer(new StringInvariantIgnoreCaseComparerAscending())
            .SetDataDirectory(dataPath)
            .SetKeySerializer(new Utf8StringSerializer())
            .OpenOrCreate();
        _maintainer = _zoneTree.CreateMaintainer();
        _maintainer.EnableJobForCleaningInactiveCaches = true;
        NotifyPublish += OnMessagePublished;
        NotifySubscribe += OnMessageSubscribed;
    }

    /// <summary>
    /// Publishes a message to a queue
    /// </summary>
    /// <param name="queue">The destination queue</param>
    /// <param name="payload">The message payload</param>
    /// <returns>Message id</returns>
    public Message Publish(string queue, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var normalizedName = queue.Trim().ToLowerInvariant();

        var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

        var message = new Message(Guid.NewGuid(), normalizedName, payload, timestamp);

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
    public IAsyncEnumerable<Message?> Subscribe(string queue, bool exclusive, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);

        var subscription = new Subscription(Guid.NewGuid(),
            new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), exclusive);

        var topicTrimmedLowered = queue.Trim().ToLowerInvariant();

        var subscriptions = _subscriptions.GetOrAdd(topicTrimmedLowered, _ => []);

        subscriptions.Add(subscription);

        if (!_topicIndex.ContainsKey(topicTrimmedLowered))
        {
            var addedTopicToIndexResult = _topicIndex.TryAdd(topicTrimmedLowered, 0);
            _logger.LogDebug("Add topic '{topic}' to index result: {addedTopicToIndexResult}", topicTrimmedLowered,
                addedTopicToIndexResult);
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

            if (!_topicIndex.TryGetValue(topicTrimmedLowered, out var currentIndex)) return;
            currentIndex = _subscriptions[topicTrimmedLowered].Count > 0
                ? (currentIndex + 1) % _subscriptions[topicTrimmedLowered].Count
                : 0;
            _topicIndex[topicTrimmedLowered] = currentIndex;
        });

        NotifySubscribe?.Invoke(this, topicTrimmedLowered);

        return subscription.GetNextAvailableMessageAsync(cancellationToken);
    }

    /// <summary>
    /// Reset the content of a queue
    /// </summary>
    /// <param name="queue"></param>
    /// <returns></returns>
    public bool Reset(string queue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        _semaphore.Wait();
        try
        {
            var queueNormalized = queue.ToLower().Trim();
            var deleteResult = _zoneTree.TryDelete(queueNormalized, out _);
            _logger.LogDebug("Delete result: {deleteResult}", deleteResult);
            return deleteResult;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        NotifyPublish -= OnMessagePublished;
        NotifySubscribe -= OnMessageSubscribed;
        _maintainer.Dispose();
        _zoneTree.Dispose();
    }

    private async void OnMessagePublished(object source, Message message)
    {
        try
        {
            await _semaphore.WaitAsync();
            try
            {
                var messages = new List<Message>();
                if (_zoneTree.TryGet(message.Queue, out var storedBytes))
                {
                    if (storedBytes.Length > 0)
                        messages = MessagePackSerializer.Deserialize<List<Message>>(storedBytes);
                }

                messages.Add(message);
                var serializedQueue = MessagePackSerializer.Serialize(messages);
                var upsertResult = _zoneTree.Upsert(message.Queue, serializedQueue);
                _logger.LogDebug(
                    "Added message '{id}' for queue '{queue}' in storage with payload '{payload}' with upsertResult {upsertResult}",
                    message.Id, message.Queue, message.Payload, upsertResult);
                await _maintainer.WaitForBackgroundThreadsAsync();
                NotifySubscribe?.Invoke(this, message.Queue);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception e)
        {
            _logger.LogError("An error '{error}' has occurred during OnMessagePublished", e.Message);
        }
    }

    private async void OnMessageSubscribed(object source, string queue)
    {
        try
        {
            await _semaphore.WaitAsync();

            try
            {
                var subscription = GetNextSubscription(queue);

                if (subscription == null)
                {
                    _logger.LogInformation(
                        "No active subscriptions found for queue '{queue}'. No processing will be done.", queue);
                    return;
                }

                if (!_zoneTree.TryGet(queue, out var storedBytes)) return;
                var messages = MessagePackSerializer.Deserialize<List<Message>>(storedBytes);
                if (messages.Count <= 0) return;
                var message = messages[0];
                messages.RemoveAt(0);
                await subscription.WriteMessageAsync(message, CancellationToken.None);
                _logger.LogInformation(
                    "Message '{messageId}' sent to '{subscriptionId}' at {timestamp} for queue '{queue}'",
                    message.Id, subscription.Id,
                    new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(), message.Queue);
                if (messages.Count != 0)
                {
                    var updatedQueue = MessagePackSerializer.Serialize(messages);
                    _zoneTree.Upsert(queue, updatedQueue);
                    await _maintainer.WaitForBackgroundThreadsAsync();
                    NotifySubscribe?.Invoke(this, queue);
                }
                else
                {
                    var deletedQueue = _zoneTree.TryDelete(queue, out var optIndex);
                    _logger.LogDebug("Queue '{queue}' deleted '{deleted}' with optIndex {optIndex}", queue,
                        deletedQueue, optIndex);
                    await _maintainer.WaitForBackgroundThreadsAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception e)
        {
            _logger.LogError("An error '{error}' has occurred during OnMessageSubscribed", e.Message);
        }
    }

    private Subscription? GetNextSubscription(string queue)
    {
        if (_subscriptions.IsEmpty)
        {
            _logger.LogWarning("No subscriptions available.");
            return null;
        }

        if (!_subscriptions.TryGetValue(queue, out var subscriptions))
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

[MessagePackObject]
public record Message(
    [property: Key(0)] Guid Id,
    [property: Key(1)] string Queue,
    [property: Key(2)] string? Payload,
    [property: Key(4)] long Timestamp);

internal record Subscription(Guid Id, long Timestamp, bool Exclusive)
{
    private readonly Channel<Message> _messageChannel = Channel.CreateBounded<Message>(1);

    internal async ValueTask WriteMessageAsync(Message message, CancellationToken cancellationToken) =>
        await _messageChannel.Writer.WriteAsync(message, cancellationToken);

    internal IAsyncEnumerable<Message?> GetNextAvailableMessageAsync(CancellationToken cancellationToken) =>
        _messageChannel.Reader.ReadAllAsync(cancellationToken);
}