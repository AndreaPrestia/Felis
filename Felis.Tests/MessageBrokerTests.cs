using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Felis.Tests;

public class MessageBrokerTests : IDisposable
{
    private readonly IHost _host;
    private readonly MessageBroker _messageBroker;
    private readonly ITestOutputHelper _testOutputHelper;
    private const string QueueName = "test-queue";

    public MessageBrokerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _host = Host.CreateDefaultBuilder()
            .AddFelisBroker()
            .Build();
        
        _messageBroker = _host.Services.GetRequiredService<MessageBroker>();
    }

    [Theory]
    [Trait("Category", "Order")]
    [InlineData(20)]
    public async Task PublishAndSubscribe_ShouldMaintainOrder(int numberOfMessages)
    {
        // Arrange
        var messagesToSend = Enumerable.Range(0, numberOfMessages).Select(s => $"Message{s + 1}").ToList();
        var receivedMessages = new List<MessageModel>(messagesToSend.Count);
        var sentMessages = new List<MessageModel>(messagesToSend.Count);
        var cts = new CancellationTokenSource();

        var deletedItems = await _messageBroker.ResetAsync(QueueName, cts.Token);
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
        foreach (var msg in messagesToSend)
        {
            var sentMessage = await _messageBroker.PublishAsync(QueueName, msg, cts.Token);
            sentMessages.Add(sentMessage);
        }

        // Wait for subscription to complete
        await subscriberTask;

        // Assert
        Assert.NotNull(sentMessages);
        Assert.NotNull(receivedMessages);
        Assert.Equal(messagesToSend.Count, receivedMessages.Count);
        Assert.Equal(sentMessages, receivedMessages);
    }

    public void Dispose()
    {
        _messageBroker.Dispose();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }
}