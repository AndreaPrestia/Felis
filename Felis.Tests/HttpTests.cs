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

        _host = Host.CreateDefaultBuilder().AddFelisBroker()
            .WithHttp(_certificate, Port)
            .Build();

        Task.Run(async () => await _host.RunAsync());
    }

    [Theory]
    [Trait("Category", "Order")]
    [InlineData(20)]
    public async Task PublishAndSubscribe_ShouldMaintainOrder(int numberOfMessages)
    {
        // Arrange
        var messagesToSend = Enumerable.Range(0, numberOfMessages).Select(s => $"HttpMessage{s + 1}").ToList();
        var sentMessages = new List<MessageModel>(messagesToSend.Count);
        var receivedMessages = new List<MessageModel>(messagesToSend.Count);
        var cts = new CancellationTokenSource();

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
                    HttpCompletionOption.ResponseHeadersRead, cts.Token);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
                using var reader = new StreamReader(stream);
                while (!reader.EndOfStream)
                {
                    var content = await reader.ReadLineAsync(cts.Token);
                    if (content != null)
                    {
                        var messageReceived = JsonSerializer.Deserialize<MessageModel?>(content);

                        if (messageReceived != null)
                        {
                            receivedMessages.Add(messageReceived);
                        }
                    }

                    if (receivedMessages.Count == messagesToSend.Count)
                    {
                        await Task.Delay(200, cts.Token);
                        break;
                    }
                }

                await cts.CancelAsync();
            }
            catch (Exception ex)
            {
                _testOutputHelper.WriteLine(ex.ToString());
            }
        }, cts.Token);

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

    private async Task<MessageModel?> PublishAsync(string message, string queue, CancellationToken cancellationToken)
    {
        var content = JsonContent.Create(message);
        var publishResponse = await _publishClient.PostAsync($"{_brokerUrl}/{queue}", content, cancellationToken);
        publishResponse.EnsureSuccessStatusCode();
        return await publishResponse.Content.ReadFromJsonAsync<MessageModel?>(cancellationToken);
    }

    public void Dispose()
    {
        _certificate.Dispose();
        _publishClient.Dispose();
        _host.Dispose();
        GC.SuppressFinalize(this);
    }
}