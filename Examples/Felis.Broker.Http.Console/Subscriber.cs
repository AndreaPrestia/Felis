using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Felis.Broker.Http.Console;

public class Subscriber : BackgroundService
{
    private readonly ILogger<Subscriber> _logger;
    private readonly X509Certificate2 _certificate;
    private readonly string _brokerUrl;

    public Subscriber(ILogger<Subscriber> logger, X509Certificate2 certificate, string brokerUrl)
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
                var taskGeneric =
                    SubscribeInParallelAsync(_brokerUrl, _certificate, 20, "Generic", false, stoppingToken);
                var taskExclusive =
                    SubscribeInParallelAsync(_brokerUrl, _certificate, 1, "Exclusive", true, stoppingToken);
                await Task.WhenAll(new[] { taskGeneric, taskExclusive });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in subscriber: '{0}'", ex.Message);
            }
            finally
            {
                _logger.LogInformation("Terminated subscriber");
            }
        }
    }

    private async Task SubscribeInParallelAsync(string brokerUrl, X509Certificate2 clientCertificate,
        int numberOfSubscribers, string queue, bool exclusive, CancellationToken cancellationToken)
    {
        try
        {
            var subscriberTasks = new Task[numberOfSubscribers];
            for (var i = 0; i < numberOfSubscribers; i++)
            {
                var subscriberId = i + 1;
                subscriberTasks[i] = Task.Run(() =>
                    SubscribeAsync(brokerUrl, clientCertificate, subscriberId, queue, exclusive, cancellationToken));
            }

            await Task.WhenAll(subscriberTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in subscriber: '{0}'", ex.Message);
        }
    }

    private async Task SubscribeAsync(string brokerUrl, X509Certificate2 clientCertificate, int subscriberId,
        string queue, bool exclusive, CancellationToken cancellationToken)
    {
        var handler = new HttpClientHandler
        {
            ClientCertificates = { clientCertificate },
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        using var client = new HttpClient(handler);
        client.DefaultRequestVersion = new Version(3, 0);
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        client.DefaultRequestHeaders.TryAddWithoutValidation("x-exclusive", exclusive.ToString());

        try
        {
            using var response = await client.GetAsync($"{brokerUrl}/{queue}", HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                var content = await reader.ReadLineAsync(cancellationToken);
                _logger.LogInformation($"Received message for subscriber {subscriberId} - {queue}: {content}");
            }
        }
        catch (HttpRequestException e)
        {
            _logger.LogWarning($"Subscriber {subscriberId} request error: {e.Message}");
        }
    }
}