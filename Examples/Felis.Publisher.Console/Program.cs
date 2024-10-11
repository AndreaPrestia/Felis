using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;

try
{
    Console.WriteLine("Started Felis.Publisher.Console");

    string pfxPath = "Output.pfx";
    string pfxPassword = "Password.1";

    var clientCertificate = new X509Certificate2(pfxPath, pfxPassword);

    while (true)
    {
        var taskGeneric = PublishInParallelAsync(clientCertificate, 20, "Generic", 0, false);
        var taskTTL = PublishInParallelAsync(clientCertificate, 20, "TTL", 5, false);
        var taskBroadcast = PublishInParallelAsync(clientCertificate, 20, "Broadcast", 0, true);
        var taskExclusive = PublishInParallelAsync(clientCertificate, 20, "Exclusive", 0, false);
        await Task.WhenAll([taskGeneric, taskTTL, taskBroadcast, taskExclusive]);
        Console.WriteLine("Publish finished, waiting 5 seconds to next round");
        Thread.Sleep(5000);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error in Felis.Subscriber.Console: {error}", ex.Message);
}
finally
{
    Console.WriteLine("Terminated Felis.Publisher.Console");
}

static async Task PublishInParallelAsync(X509Certificate2 clientCertificate, int numberOfPublishers, string topic, int ttl, bool broadcast)
{
    try
    {
        Task[] publisherTasks = new Task[numberOfPublishers];
        for (int i = 0; i < numberOfPublishers; i++)
        {
            publisherTasks[i] = Task.Run(() => PublishAsync(clientCertificate, topic, ttl, broadcast));
        }

        await Task.WhenAll(publisherTasks);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Error in Felis.Publisher.Console: {error}", ex.Message);
    }
}

static async Task PublishAsync(X509Certificate2 clientCertificate, string topic, int ttl, bool broadcast)
{
    // Create an HttpClientHandler with mutual TLS authentication and disabled server cert validation
    var handler = new HttpClientHandler
    {
        ClientCertificates = { clientCertificate },
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };

    using var client = new HttpClient(handler);
    client.DefaultRequestVersion = new Version(3, 0);
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
    client.DefaultRequestHeaders.TryAddWithoutValidation("x-ttl", ttl.ToString());
    client.DefaultRequestHeaders.TryAddWithoutValidation("x-broadcast", broadcast.ToString());

    try
    {
        using var response = await client.PostAsJsonAsync($"https://localhost:7110/{topic}", new
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
