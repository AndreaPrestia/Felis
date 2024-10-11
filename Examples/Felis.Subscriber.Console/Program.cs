using System.Security.Cryptography.X509Certificates;

try
{
    Console.WriteLine("Started Felis.Subscriber.Console");

    string pfxPath = "Output.pfx";
    string pfxPassword = "Password.1";

    var clientCertificate = new X509Certificate2(pfxPath, pfxPassword);

    var taskGeneric = SubscribeInParallelAsync(clientCertificate, 20, "Generic", false);
    var taskTTL = SubscribeInParallelAsync(clientCertificate, 10, "TTL", false);
    var taskBroadcast = SubscribeInParallelAsync(clientCertificate, 10, "Broadcast", false);
    var taskExclusive = SubscribeInParallelAsync(clientCertificate, 1, "Exclusive", true);
    await Task.WhenAll([taskGeneric, taskTTL, taskBroadcast, taskExclusive]);
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error in Felis.Subscriber.Console: {error}", ex.Message);
}
finally
{
    Console.WriteLine("Terminated Felis.Subscriber.Console");
}

static async Task SubscribeInParallelAsync(X509Certificate2 clientCertificate, int numberOfSubscribers, string topic, bool exclusive)
{
    try
    {
        Task[] subscriberTasks = new Task[numberOfSubscribers];
        for (int i = 0; i < numberOfSubscribers; i++)
        {
            int subscriberId = i + 1;
            subscriberTasks[i] = Task.Run(() => SubscribeAsync(clientCertificate, subscriberId, topic, exclusive));
        }

        await Task.WhenAll(subscriberTasks);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Error in Felis.Subscriber.Console: {error}", ex.Message);
    }
}

static async Task SubscribeAsync(X509Certificate2 clientCertificate, int subscriberId, string topic, bool exclusive)
{
    var handler = new HttpClientHandler
    {
        ClientCertificates = { clientCertificate },
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };

    using var client = new HttpClient(handler);
    client.DefaultRequestVersion = new Version(3, 0); 
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
    client.DefaultRequestHeaders.TryAddWithoutValidation("x-exclusive", exclusive.ToString());

    try
    {
        using var response = await client.GetAsync($"https://localhost:7110/{topic}", HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var content = await reader.ReadLineAsync();
            Console.WriteLine($"Received message for subscriber {subscriberId} - {topic}: {content}");
        }
    }
    catch (HttpRequestException e)
    {
        Console.WriteLine($"Subscriber {subscriberId} request error: {e.Message}");
    }
}
