using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Felis.Tests;

public class MessageBrokerTests : IDisposable
{
    private readonly MessageBroker _messageBroker;
    private readonly ITestOutputHelper _testOutputHelper;
    private const string QueueName = "test-queue";

    public MessageBrokerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        var host = Host.CreateDefaultBuilder()
            .AddBroker()
            .Build();

        _messageBroker = host.Services.GetRequiredService<MessageBroker>();
    }

    [Theory]
    [Trait("Category", "Order")]
    [InlineData(20)]
    public async Task PublishAndSubscribe_ShouldMaintainOrder(int numberOfMessages)
    {
        // Arrange
        var messagesToSend = Enumerable.Range(0, numberOfMessages).Select(s => $"Message{s + 1}").ToList();
        var receivedMessages = new List<Message>(messagesToSend.Count);
        var sentMessages = new List<Message>(messagesToSend.Count);
        var cts = new CancellationTokenSource();

        var deletedItems = _messageBroker.Reset(QueueName);
        _testOutputHelper.WriteLine($"Deleted items from queue '{QueueName}': {deletedItems}");

        // Act - Start subscriber independently
        var subscriberTask = Task.Run(async () =>
        {
            await foreach (var message in _messageBroker.Subscribe(QueueName, true, cts.Token))
            {
                if (message != null)
                {
                    receivedMessages.Add(message);
                }

                if (receivedMessages.Count == messagesToSend.Count)
                {
                    break;
                }
            }

            await cts.CancelAsync();
        }, cts.Token);

        // Act - Publish messages independently
        sentMessages.AddRange(messagesToSend.Select(messageToSend => _messageBroker.Publish(QueueName, messageToSend)));

        // Wait for subscription to complete
        await subscriberTask;

        // Assert
        Assert.NotNull(sentMessages);
        Assert.NotNull(receivedMessages);
        Assert.Equal(messagesToSend.Count, receivedMessages.Count);
        Assert.Equal(sentMessages, receivedMessages);
    }

    [Theory]
    [Trait("Category", "Order")]
    [InlineData(20)]
    public async Task PublishAndSubscribe_ShouldMaintainOrderWithSubscribeDelay(int numberOfMessages)
    {
        // Arrange
        var messagesToSend = Enumerable.Range(0, numberOfMessages).Select(s => $"Message{s + 1}").ToList();
        var receivedMessages = new List<Message>(messagesToSend.Count);
        var sentMessages = new List<Message>(messagesToSend.Count);
        var cts = new CancellationTokenSource();

        var deletedItems = _messageBroker.Reset(QueueName);
        _testOutputHelper.WriteLine($"Deleted items from queue '{QueueName}': {deletedItems}");

        // Act - Start subscriber independently
        var subscriberTask = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
            await foreach (var message in _messageBroker.Subscribe(QueueName, true, cts.Token))
            {
                if (message != null)
                {
                    receivedMessages.Add(message);
                }

                if (receivedMessages.Count == messagesToSend.Count)
                {
                    break;
                }
            }

            await cts.CancelAsync();
        }, cts.Token);

        // Act - Publish messages independently
        sentMessages.AddRange(messagesToSend.Select(messageToSend => _messageBroker.Publish(QueueName, messageToSend)));

        // Wait for subscription to complete
        await subscriberTask;

        // Assert
        Assert.NotNull(sentMessages);
        Assert.NotNull(receivedMessages);
        Assert.Equal(messagesToSend.Count, receivedMessages.Count);
        Assert.Equal(sentMessages, receivedMessages);
    }

    [Theory]
    [Trait("Category", "Order")]
    [InlineData(4, 4)]
    public async Task PublishAndSubscribe_ShouldMaintainOrderMultipleSubscribers(int numberOfMessages,
        int numberOfSubscribers)
    {
        // Arrange
        var messagesToSend = Enumerable.Range(0, numberOfMessages).Select(s => $"Message{s + 1}").ToList();
        var receivedMessages = new ConcurrentBag<Message>();
        var sentMessages = new List<Message>(messagesToSend.Count);
        var receivedMessagesBySubscriber = new ConcurrentDictionary<int, List<Message>>();
        var subscribersTask = new List<Task>();

        var deletedItems = _messageBroker.Reset(QueueName);
        _testOutputHelper.WriteLine($"Deleted items from queue '{QueueName}': {deletedItems}");

        // Initialize subscriber message storage
        for (var i = 1; i <= numberOfSubscribers; i++)
        {
            receivedMessagesBySubscriber[i] = new List<Message>();
        }

        // Act - Start subscribers
        for (var i = 0; i < numberOfSubscribers; i++)
        {
            var subscriberId = i + 1;
            var localCts = new CancellationTokenSource();

            var subscriberTask = Task.Run(async () =>
            {
                await foreach (var message in _messageBroker.Subscribe(QueueName, false, localCts.Token))
                {
                    if (message == null) continue;
                    receivedMessages.Add(message);
                    receivedMessagesBySubscriber[subscriberId].Add(message);
                    if (receivedMessagesBySubscriber[subscriberId].Count >= numberOfMessages / numberOfSubscribers)
                    {
                        break;
                    }
                }

                await localCts.CancelAsync();
            }, localCts.Token);

            subscribersTask.Add(subscriberTask);
        }

        // Act - Publish messages
        sentMessages.AddRange(messagesToSend.Select(messageToSend => _messageBroker.Publish(QueueName, messageToSend)));

        // Wait for all subscribers to finish
        await Task.WhenAll(subscribersTask);

        // Assert
        Assert.NotNull(sentMessages);
        Assert.NotNull(receivedMessages);
        Assert.Equal(messagesToSend.Count, receivedMessages.Count);
        Assert.Equal(sentMessages, receivedMessages.OrderBy(x => x.Payload).ToList());
        Assert.Equal(numberOfSubscribers, receivedMessagesBySubscriber.Count);
        Assert.True(receivedMessagesBySubscriber.All(x => x.Value.Count == numberOfMessages / numberOfSubscribers));
    }
    
    [Theory]
    [Trait("Category", "Order")]
    [InlineData(4, 4)]
    public async Task PublishAndSubscribe_ShouldMaintainOrderMultipleSubscribersWithExclusive(int numberOfMessages,
        int numberOfSubscribers)
    {
        // Arrange
        var messagesToSend = Enumerable.Range(0, numberOfMessages).Select(s => $"Message{s + 1}").ToList();
        var receivedMessages = new ConcurrentBag<Message>();
        var sentMessages = new List<Message>(messagesToSend.Count);
        var receivedMessagesBySubscriber = new ConcurrentDictionary<int, List<Message>>();
        var subscribersTask = new List<Task>();

        var deletedItems = _messageBroker.Reset(QueueName);
        _testOutputHelper.WriteLine($"Deleted items from queue '{QueueName}': {deletedItems}");

        // Initialize subscriber message storage
        for (var i = 1; i <= numberOfSubscribers; i++)
        {
            receivedMessagesBySubscriber[i] = new List<Message>();
        }

        // Act - Start subscribers
        for (var i = 0; i < numberOfSubscribers; i++)
        {
            var subscriberId = i + 1;
            var localCts = new CancellationTokenSource();
            var subscriberTask = Task.Run(async () =>
            {
                await foreach (var message in _messageBroker.Subscribe(QueueName, subscriberId == 1, localCts.Token))
                {
                    if (message == null) continue;
                    receivedMessages.Add(message);
                    receivedMessagesBySubscriber[subscriberId].Add(message);
                    if (receivedMessages.Count >= numberOfMessages)
                    {
                        break;
                    }
                }

                await localCts.CancelAsync();
            }, localCts.Token);

            subscribersTask.Add(subscriberTask);
        }

        // Act - Publish messages
        sentMessages.AddRange(messagesToSend.Select(messageToSend => _messageBroker.Publish(QueueName, messageToSend)));

        // Wait for all subscribers to finish
        await Task.WhenAny(subscribersTask);

        // Assert
        Assert.NotNull(sentMessages);
        Assert.NotNull(receivedMessages);
        Assert.Equal(messagesToSend.Count, receivedMessages.Count);
        Assert.Equal(sentMessages, receivedMessages.OrderBy(x => x.Payload).ToList());
        Assert.Equal(numberOfSubscribers, receivedMessagesBySubscriber.Count);
        Assert.Equal(sentMessages, receivedMessagesBySubscriber[1].OrderBy(x => x.Payload).ToList());
        Assert.Empty(receivedMessagesBySubscriber.Where(x => x.Key != 1).SelectMany(e => e.Value));
    }

    public void Dispose()
    {
        _messageBroker.Dispose();
        GC.SuppressFinalize(this);
    }
}