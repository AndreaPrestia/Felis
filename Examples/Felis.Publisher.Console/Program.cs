using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;

try
{
    Console.WriteLine("Started Felis.Publisher.Console");

    const string pfxPath = "Output.pfx";
    const string pfxPassword = "Password.1";

    var clientCertificate = new X509Certificate2(pfxPath, pfxPassword);
    var brokerUrl = Environment.GetEnvironmentVariable("BROKER_URL") ?? "https://localhost:7110";

    while (true)
    {
        var taskGeneric = PublishInParallelAsync(brokerUrl, clientCertificate, 20, "Generic", 0, false);
        var taskTtL = PublishInParallelAsync(brokerUrl, clientCertificate, 20, "TTL", 5, false);
        var taskBroadcast = PublishInParallelAsync(brokerUrl, clientCertificate, 20, "Broadcast", 0, true);
        var taskExclusive = PublishInParallelAsync(brokerUrl, clientCertificate, 20, "Exclusive", 0, false);
        await Task.WhenAll(new [] {taskGeneric, taskTtL, taskBroadcast, taskExclusive});
        Console.WriteLine("Publish finished, waiting 5 seconds to next round");
        Thread.Sleep(5000);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error in Felis.Subscriber.Console: '{0}'", ex.Message);
}
finally
{
    Console.WriteLine("Terminated Felis.Publisher.Console");
}

static async Task PublishInParallelAsync(string brokerUrl, X509Certificate2 clientCertificate, int numberOfPublishers, string topic, int ttl, bool broadcast)
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
        Console.Error.WriteLine("Error in Felis.Publisher.Console: '{0}'", ex.Message);
    }
}

static async Task PublishAsync(string brokerUrl, X509Certificate2 clientCertificate, string topic, int ttl, bool broadcast)
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
        Console.WriteLine($"Publisher request error: {e.Message}");
    }
}
