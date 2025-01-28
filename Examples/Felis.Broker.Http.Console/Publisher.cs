using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Felis.Broker.Http.Console;

public class Publisher : BackgroundService
{
    private readonly ILogger<Publisher> _logger;
    private readonly X509Certificate2 _certificate;
    private readonly string _brokerUrl;

    public Publisher(ILogger<Publisher> logger, X509Certificate2 certificate, string brokerUrl)
    {
        _logger = logger;
        _certificate = certificate;
        _brokerUrl = brokerUrl;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var taskGeneric = PublishInParallelAsync(_brokerUrl, _certificate, 20, "Generic", 0, false);
                var taskTtL = PublishInParallelAsync(_brokerUrl, _certificate, 20, "TTL", 5, false);
                var taskBroadcast = PublishInParallelAsync(_brokerUrl, _certificate, 20, "Broadcast", 0, true);
                var taskExclusive = PublishInParallelAsync(_brokerUrl, _certificate, 20, "Exclusive", 0, false);
                await Task.WhenAll(new [] {taskGeneric, taskTtL, taskBroadcast, taskExclusive});
                _logger.LogInformation("Publish finished, waiting 5 seconds to next round");
                Thread.Sleep(5000);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in publisher: '{0}'", ex.Message);
            }
            finally
            {
                _logger.LogInformation("Terminated publisher");
            }
        }
    }
    
    private async Task PublishInParallelAsync(string brokerUrl, X509Certificate2 clientCertificate, int numberOfPublishers, string topic, int ttl, bool broadcast)
    {
        try
        {
            var publisherTasks = new Task[numberOfPublishers];
            for (var i = 0; i < numberOfPublishers; i++)
            {
                publisherTasks[i] = Task.Run(() => PublishAsync(brokerUrl, clientCertificate, topic, ttl, broadcast));
            }

            await Task.WhenAll(publisherTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in publisher: '{0}'", ex.Message);
        }
    }

    private async Task PublishAsync(string brokerUrl, X509Certificate2 clientCertificate, string topic, int ttl, bool broadcast)
    {
        var handler = new HttpClientHandler
        {
            ClientCertificates = { clientCertificate },
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        using var client = new HttpClient(handler);
        client.DefaultRequestVersion = new Version(3, 0);
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        client.DefaultRequestHeaders.TryAddWithoutValidation("x-ttl", ttl.ToString());
        client.DefaultRequestHeaders.TryAddWithoutValidation("x-broadcast", broadcast.ToString());

        try
        {
            using var response = await client.PostAsJsonAsync($"{brokerUrl}/{topic}", new
            {
                description = $"{topic} at {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()}"
            });
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            _logger.LogError($"Publisher request error: {e.Message}");
        }
    }
}