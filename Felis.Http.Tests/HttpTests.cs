using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Felis.Http.Tests;

public class HttpTests : IDisposable
{
    private readonly IHost _host;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _brokerUrl;
    private readonly HttpClient _publishClient;
    private const int Port = 7110;
    private const string QueueName = "test-http-queue";

    public HttpTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _brokerUrl = $"https://localhost:{Port}";

        _publishClient = new HttpClient();
        _publishClient.DefaultRequestVersion = new Version(3, 0);
        _publishClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

        _host = Host.CreateDefaultBuilder()
            .AddBroker()
            .WithHttps(port: Port)
            .Build();

        _host.Start();
    }

    [Theory]
    [Trait("Category", "Order")]
    [InlineData(20)]
    public async Task PublishAndSubscribe_ShouldMaintainOrder(int numberOfMessages)
    {
        // Arrange
        var messagesToSend = Enumerable.Range(0, numberOfMessages).Select(s => $"HttpMessage{s + 1}").ToList();
        var sentMessages = new List<Message>(messagesToSend.Count);
        var receivedMessages = new List<Message>(messagesToSend.Count);
        var cts = new CancellationTokenSource();

        var deletedItems = await ResetAsync(QueueName, cts.Token);
        _testOutputHelper.WriteLine($"Deleted items from queue '{QueueName}': {deletedItems}");

        var subscriberCts = new CancellationTokenSource();
        // Act - Start subscriber (HTTP2 keep-alive GET)
        var subscriberTask = Task.Run(async () =>
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestVersion = new Version(3, 0);
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                client.DefaultRequestHeaders.TryAddWithoutValidation("x-exclusive", "false");

                using var response = await client.GetAsync($"{_brokerUrl}/{QueueName}",
                    HttpCompletionOption.ResponseHeadersRead, subscriberCts.Token);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(subscriberCts.Token);
                using var reader = new StreamReader(stream);
                while (!reader.EndOfStream)
                {
                    var content = await reader.ReadLineAsync(subscriberCts.Token);
                    if (content != null)
                    {
                        var messageReceived = JsonSerializer.Deserialize<Message?>(content);

                        if (messageReceived != null)
                        {
                            receivedMessages.Add(messageReceived);
                        }
                    }

                    if (receivedMessages.Count == messagesToSend.Count)
                    {
                        break;
                    }
                }

                await subscriberCts.CancelAsync();
            }
            catch (Exception ex)
            {
                _testOutputHelper.WriteLine(ex.ToString());
            }
        }, subscriberCts.Token);

        // Act - Publish messages via HTTP POST
        foreach (var msg in messagesToSend)
        {
            var sentMessage = await PublishAsync(msg, QueueName, cts.Token);
            if (sentMessage != null)
            {
                sentMessages.Add(sentMessage);
            }
        }

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
        var cts = new CancellationTokenSource();
        var subscribersTask = new List<Task>();

        var deletedItems = await ResetAsync(QueueName, cts.Token);
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
                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestVersion = new Version(3, 0);
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                    client.DefaultRequestHeaders.TryAddWithoutValidation("x-exclusive", "false");

                    using var response = await client.GetAsync($"{_brokerUrl}/{QueueName}",
                        HttpCompletionOption.ResponseHeadersRead, localCts.Token);
                    response.EnsureSuccessStatusCode();
                    await using var stream = await response.Content.ReadAsStreamAsync(localCts.Token);
                    using var reader = new StreamReader(stream);
                    while (!reader.EndOfStream)
                    {
                        var content = await reader.ReadLineAsync(localCts.Token);
                        if (content != null)
                        {
                            var messageReceived = JsonSerializer.Deserialize<Message?>(content);

                            if (messageReceived != null)
                            {
                                receivedMessages.Add(messageReceived);
                                receivedMessagesBySubscriber[subscriberId].Add(messageReceived);
                            }
                        }

                        if (receivedMessagesBySubscriber[subscriberId].Count >= numberOfMessages / numberOfSubscribers)
                        {
                            break;
                        }
                    }

                    await localCts.CancelAsync();
                }
                catch (Exception ex)
                {
                    _testOutputHelper.WriteLine(ex.ToString());
                }
            }, localCts.Token);

            subscribersTask.Add(subscriberTask);
        }

        // Act - Publish messages via HTTP POST
        foreach (var msg in messagesToSend)
        {
            var sentMessage = await PublishAsync(msg, QueueName, cts.Token);
            if (sentMessage != null)
            {
                sentMessages.Add(sentMessage);
            }
        }

        // Act - Start subscriber (HTTP2 keep-alive GET)
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
        var cts = new CancellationTokenSource();
        var subscribersTask = new List<Task>();

        var deletedItems = await ResetAsync(QueueName, cts.Token);
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
                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestVersion = new Version(3, 0);
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                    client.DefaultRequestHeaders.TryAddWithoutValidation("x-exclusive", subscriberId == 1 ? "true" : "false");

                    using var response = await client.GetAsync($"{_brokerUrl}/{QueueName}",
                        HttpCompletionOption.ResponseHeadersRead, localCts.Token);
                    response.EnsureSuccessStatusCode();
                    await using var stream = await response.Content.ReadAsStreamAsync(localCts.Token);
                    using var reader = new StreamReader(stream);
                    while (!reader.EndOfStream)
                    {
                        var content = await reader.ReadLineAsync(localCts.Token);
                        if (content != null)
                        {
                            var messageReceived = JsonSerializer.Deserialize<Message?>(content);

                            if (messageReceived != null)
                            {
                                receivedMessages.Add(messageReceived);
                                receivedMessagesBySubscriber[subscriberId].Add(messageReceived);
                            }
                        }

                        if (receivedMessagesBySubscriber[subscriberId].Count >= numberOfMessages)
                        {
                            break;
                        }
                    }

                    await localCts.CancelAsync();
                }
                catch (Exception ex)
                {
                    _testOutputHelper.WriteLine(ex.ToString());
                }
            }, localCts.Token);

            subscribersTask.Add(subscriberTask);
        }

        // Act - Publish messages via HTTP POST
        foreach (var msg in messagesToSend)
        {
            var sentMessage = await PublishAsync(msg, QueueName, cts.Token);
            if (sentMessage != null)
            {
                sentMessages.Add(sentMessage);
            }
        }

        // Act - Start subscriber (HTTP2 keep-alive GET)
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

    private async Task<Message?> PublishAsync(string message, string queue, CancellationToken cancellationToken)
    {
        var content = JsonContent.Create(message);
        var publishResponse = await _publishClient.PostAsync($"{_brokerUrl}/{queue}", content, cancellationToken);
        publishResponse.EnsureSuccessStatusCode();
        return await publishResponse.Content.ReadFromJsonAsync<Message?>(cancellationToken);
    }

    private async Task<bool> ResetAsync(string queue, CancellationToken cancellationToken)
    {
        var resetResponse = await _publishClient.DeleteAsync($"{_brokerUrl}/{queue}", cancellationToken);
        resetResponse.EnsureSuccessStatusCode();
        var deletedItems = await resetResponse.Content.ReadAsStringAsync(cancellationToken);
        return !string.IsNullOrEmpty(deletedItems) && bool.Parse(deletedItems);
    }

    public void Dispose()
    {
        _publishClient.Dispose();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }
}