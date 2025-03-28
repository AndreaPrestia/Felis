﻿using System.Collections.Concurrent;
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
            .AddBroker("FelisHttp")
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
    [InlineData(20, 5)]
    public async Task PublishAndSubscribe_ShouldMaintainOrderDelay(int numberOfMessages, int delayInSeconds)
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
                await Task.Delay(TimeSpan.FromSeconds(delayInSeconds), cts.Token);
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
            receivedMessagesBySubscriber[i] = [];
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

                        if (receivedMessages.Count < numberOfMessages) continue;
                        await localCts.CancelAsync();
                        break;
                    }
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
        lock (receivedMessages)
        {
            Assert.Equal(sentMessages, receivedMessages.Reverse().ToList());
        }
        Assert.Equal(numberOfSubscribers, receivedMessagesBySubscriber.Count);
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
        var sharedCts = new CancellationTokenSource();
        var subscribersTask = new List<Task>();

        var deletedItems = await ResetAsync(QueueName, cts.Token);
        _testOutputHelper.WriteLine($"Deleted items from queue '{QueueName}': {deletedItems}");

        // Initialize subscriber message storage
        for (var i = 1; i <= numberOfSubscribers; i++)
        {
            receivedMessagesBySubscriber[i] = [];
        }

        // Act - Start subscribers
        for (var i = 0; i < numberOfSubscribers; i++)
        {
            var subscriberId = i + 1;
            var subscriberTask = Task.Run(async () =>
            {
                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestVersion = new Version(3, 0);
                    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
                    client.DefaultRequestHeaders.TryAddWithoutValidation("x-exclusive",
                        subscriberId == 1 ? "true" : "false");

                    using var response = await client.GetAsync($"{_brokerUrl}/{QueueName}",
                        HttpCompletionOption.ResponseHeadersRead, sharedCts.Token);
                    response.EnsureSuccessStatusCode();
                    await using var stream = await response.Content.ReadAsStreamAsync(sharedCts.Token);
                    using var reader = new StreamReader(stream);
                    while (!reader.EndOfStream && !sharedCts.Token.IsCancellationRequested)
                    {
                        var content = await reader.ReadLineAsync(sharedCts.Token);
                        if (content != null)
                        {
                            var messageReceived = JsonSerializer.Deserialize<Message?>(content);

                            if (messageReceived != null)
                            {
                                receivedMessages.Add(messageReceived);
                                receivedMessagesBySubscriber[subscriberId].Add(messageReceived);
                            }
                        }

                        if (receivedMessagesBySubscriber[subscriberId].Count < numberOfMessages) continue;
                        await sharedCts.CancelAsync();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _testOutputHelper.WriteLine(ex.ToString());
                }
            }, sharedCts.Token);

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
        lock (receivedMessages)
        {
            Assert.Equal(sentMessages, receivedMessages.Reverse().ToList());
        }
        Assert.Equal(numberOfSubscribers, receivedMessagesBySubscriber.Count);
    }

    private async Task<Message?> PublishAsync(string message, string queue, CancellationToken cancellationToken)
    {
        var content = JsonContent.Create(message);
        var publishResponse = await _publishClient.PostAsync($"{_brokerUrl}/{queue}", content, cancellationToken);
        var responseContent = await publishResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!publishResponse.IsSuccessStatusCode)
        {
            _testOutputHelper.WriteLine(publishResponse.ReasonPhrase);
            _testOutputHelper.WriteLine(responseContent);
        }
        publishResponse.EnsureSuccessStatusCode();
        return await publishResponse.Content.ReadFromJsonAsync<Message?>(cancellationToken);
    }

    private async Task<bool> ResetAsync(string queue, CancellationToken cancellationToken)
    {
        var resetResponse = await _publishClient.DeleteAsync($"{_brokerUrl}/{queue}", cancellationToken);
        var responseContent = await resetResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!resetResponse.IsSuccessStatusCode)
        {
            _testOutputHelper.WriteLine(resetResponse.ReasonPhrase);
            _testOutputHelper.WriteLine(responseContent);
        }
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