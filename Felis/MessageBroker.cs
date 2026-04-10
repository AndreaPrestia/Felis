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
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _queueLocks = new();
    private readonly ConcurrentDictionary<string, long> _queueWriteSeq = new();
    private readonly ConcurrentDictionary<string, long> _queueReadSeq = new();
    private readonly int _channelCapacity;

    internal MessageBroker(MessageBrokerOptions options, ILogger<MessageBroker> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.DataPath);
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _channelCapacity = options.ChannelCapacity;
        _zoneTree = new ZoneTreeFactory<string, Memory<byte>>()
            .SetComparer(new StringInvariantIgnoreCaseComparerAscending())
            .SetDataDirectory(options.DataPath)
            .SetKeySerializer(new Utf8StringSerializer())
            .OpenOrCreate();
        _maintainer = _zoneTree.CreateMaintainer();
        _maintainer.EnableJobForCleaningInactiveCaches = true;
        InitializeSequences();
    }

    private void InitializeSequences()
    {
        using var iterator = _zoneTree.CreateIterator();
        iterator.SeekFirst();
        while (iterator.HasCurrent)
        {
            var key = iterator.CurrentKey;
            var separatorIndex = key.IndexOf('|');
            if (separatorIndex >= 0)
            {
                var queueName = key[..separatorIndex];
                var seqStr = key[(separatorIndex + 1)..];
                if (long.TryParse(seqStr, out var seq))
                {
                    _queueWriteSeq.AddOrUpdate(queueName, seq, (_, existing) => Math.Max(existing, seq));
                    _queueReadSeq.AddOrUpdate(queueName, seq - 1, (_, existing) => Math.Min(existing, seq - 1));
                }
            }
            iterator.Next();
        }
    }

    private SemaphoreSlim GetQueueLock(string queue) =>
        _queueLocks.GetOrAdd(queue, _ => new SemaphoreSlim(1, 1));

    private static string MakeKey(string queue, long seq) => $"{queue}|{seq:D20}";

    /// <summary>
    /// Publishes a message to a queue
    /// </summary>
    /// <param name="queue">The destination queue</param>
    /// <param name="payload">The message payload</param>
    /// <param name="ttl">The message time to live in seconds</param>
    /// <returns>The published message</returns>
    public async Task<Message> PublishAsync(string queue, string payload, int? ttl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var normalizedQueue = queue.Trim().ToLowerInvariant();
        var message = new Message(Guid.NewGuid(), normalizedQueue, payload,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ttl is > 0 ? DateTimeOffset.UtcNow.AddSeconds(ttl.Value).ToUnixTimeMilliseconds() : null);

        var queueLock = GetQueueLock(normalizedQueue);
        await queueLock.WaitAsync();
        try
        {
            var seq = _queueWriteSeq.AddOrUpdate(normalizedQueue, 1, (_, v) => v + 1);
            _queueReadSeq.TryAdd(normalizedQueue, 0);
            _zoneTree.Upsert(MakeKey(normalizedQueue, seq), MessagePackSerializer.Serialize(message));
            _logger.LogDebug(
                "Added message '{id}' for queue '{queue}' in storage with payload '{payload}'",
                message.Id, normalizedQueue, message.Payload);
        }
        finally
        {
            queueLock.Release();
        }

        _ = DeliverPendingMessagesAsync(normalizedQueue);

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
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), exclusive, _channelCapacity);

        var normalizedQueue = queue.Trim().ToLowerInvariant();

        var subscriptions = _subscriptions.GetOrAdd(normalizedQueue, _ => []);
        subscriptions.Add(subscription);

        _topicIndex.TryAdd(normalizedQueue, 0);

        _logger.LogInformation(
            "Subscribed '{id}' to queue '{queue}' at {timestamp}", subscription.Id, normalizedQueue,
            subscription.Timestamp);

        cancellationToken.Register(() =>
        {
            subscription.MarkCancelled();
            _logger.LogInformation(
                "UnSubscribed '{id}' from queue '{queue}'",
                subscription.Id, normalizedQueue);
        });

        _ = DeliverPendingMessagesAsync(normalizedQueue);

        return subscription.GetNextAvailableMessageAsync(cancellationToken);
    }

    /// <summary>
    /// Reset the content of a queue
    /// </summary>
    /// <param name="queue"></param>
    /// <returns></returns>
    public async Task<bool> ResetAsync(string queue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        var normalizedQueue = queue.Trim().ToLowerInvariant();
        var queueLock = GetQueueLock(normalizedQueue);
        await queueLock.WaitAsync();
        try
        {
            var writeSeq = _queueWriteSeq.GetValueOrDefault(normalizedQueue, 0);
            var readSeq = _queueReadSeq.GetValueOrDefault(normalizedQueue, 0);
            var deleted = false;

            for (var seq = readSeq + 1; seq <= writeSeq; seq++)
            {
                if (_zoneTree.TryDelete(MakeKey(normalizedQueue, seq), out _))
                    deleted = true;
            }

            _queueReadSeq[normalizedQueue] = writeSeq;
            _logger.LogDebug("Reset queue '{queue}': deleted up to seq {seq}", normalizedQueue, writeSeq);
            return deleted;
        }
        finally
        {
            queueLock.Release();
        }
    }

    public void Dispose()
    {
        _maintainer.Dispose();
        _zoneTree.Dispose();
    }

    private async Task DeliverPendingMessagesAsync(string queue)
    {
        try
        {
            var queueLock = GetQueueLock(queue);
            await queueLock.WaitAsync();
            try
            {
                var subscription = GetNextSubscription(queue);
                if (subscription == null) return;

                var message = DequeueFirstValidMessage(queue);
                if (message == null) return;

                await subscription.WriteMessageAsync(message, CancellationToken.None);
                _logger.LogInformation(
                    "Message '{messageId}' sent to '{subscriptionId}' at {timestamp} for queue '{queue}'",
                    message.Id, subscription.Id,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), message.Queue);
            }
            finally
            {
                queueLock.Release();
            }

            _ = DeliverPendingMessagesAsync(queue);
        }
        catch (Exception e)
        {
            _logger.LogError("An error '{error}' has occurred during delivery for queue '{queue}'", e.Message, queue);
        }
    }

    private Message? DequeueFirstValidMessage(string queue)
    {
        var writeSeq = _queueWriteSeq.GetValueOrDefault(queue, 0);
        var readSeq = _queueReadSeq.GetValueOrDefault(queue, 0);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        while (readSeq < writeSeq)
        {
            readSeq++;
            var key = MakeKey(queue, readSeq);

            if (!_zoneTree.TryGet(key, out var bytes) || bytes.Length == 0)
            {
                _queueReadSeq[queue] = readSeq;
                continue;
            }

            var message = MessagePackSerializer.Deserialize<Message>(bytes);
            _zoneTree.TryDelete(key, out _);
            _queueReadSeq[queue] = readSeq;

            if (message.Expiration.HasValue && message.Expiration <= now)
            {
                _logger.LogDebug("Expired message '{id}' from queue '{queue}'", message.Id, queue);
                continue;
            }

            return message;
        }

        return null;
    }

    private Subscription? GetNextSubscription(string queue)
    {
        if (!_subscriptions.TryGetValue(queue, out var subscriptions) ||
            subscriptions is not { Count: > 0 })
            return null;

        var active = subscriptions.Where(x => !x.IsCancelled).ToList();
        if (active.Count == 0) return null;

        var exclusive = active.FirstOrDefault(x => x.Exclusive);
        if (exclusive != null) return exclusive;

        if (!_topicIndex.TryGetValue(queue, out var currentIndex))
            currentIndex = 0;

        currentIndex %= active.Count;
        var subscription = active[currentIndex];
        _topicIndex[queue] = (currentIndex + 1) % active.Count;
        return subscription;
    }
}

[MessagePackObject]
public record Message(
    [property: Key(0)] Guid Id,
    [property: Key(1)] string Queue,
    [property: Key(2)] string? Payload,
    [property: Key(4)] long Timestamp,
    [property: Key(5)] long? Expiration);

internal record Subscription(Guid Id, long Timestamp, bool Exclusive, int ChannelCapacity)
{
    private readonly Channel<Message> _messageChannel = Channel.CreateBounded<Message>(ChannelCapacity);
    private volatile bool _cancelled;

    internal bool IsCancelled => _cancelled;
    internal void MarkCancelled() => _cancelled = true;

    internal async ValueTask WriteMessageAsync(Message message, CancellationToken cancellationToken) =>
        await _messageChannel.Writer.WriteAsync(message, cancellationToken);

    internal IAsyncEnumerable<Message?> GetNextAvailableMessageAsync(CancellationToken cancellationToken) =>
        _messageChannel.Reader.ReadAllAsync(cancellationToken);
}