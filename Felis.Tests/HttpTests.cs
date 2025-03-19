using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Felis.Http;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Felis.Tests;

public class HttpTests : IDisposable
{
    private readonly IHost _host;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly X509Certificate2 _certificate;
    private readonly string _brokerUrl;
    private readonly HttpClient _publishClient;

    private const string QueueName = "test-http-queue";
    private const int Port = 7110;
    private const string CertPath = "Output.pfx";
    private const string CertPassword = "Password.1";

    public HttpTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _brokerUrl = $"https://localhost:{Port}";
        _certificate = new X509Certificate2(CertPath, CertPassword);

        var handler = new HttpClientHandler
        {
            ClientCertificates = { _certificate },
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        _publishClient = new HttpClient(handler);
        _publishClient.DefaultRequestVersion = new Version(3, 0);
        _publishClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

        _host = Host.CreateDefaultBuilder()
            .AddBroker()
            .WithHttp(_certificate, Port)
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
                var handler = new HttpClientHandler
                {
                    ClientCertificates = { _certificate },
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                };
                using var client = new HttpClient(handler);
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
        _certificate.Dispose();
        _publishClient.Dispose();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }
}