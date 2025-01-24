using System.Security.Cryptography.X509Certificates;

try
{
    Console.WriteLine("Started Felis.Subscriber.Console");

    const string pfxPath = "Output.pfx";
    const string pfxPassword = "Password.1";

    var clientCertificate = new X509Certificate2(pfxPath, pfxPassword);
    var brokerUrl = args.FirstOrDefault(a => a.StartsWith("--broker-url="))?.Split("=")[1] ?? "https://localhost:7110";

    var taskGeneric = SubscribeInParallelAsync(brokerUrl, clientCertificate, 20, "Generic", false);
    var taskTtL = SubscribeInParallelAsync(brokerUrl, clientCertificate, 10, "TTL", false);
    var taskBroadcast = SubscribeInParallelAsync(brokerUrl, clientCertificate, 10, "Broadcast", false);
    var taskExclusive = SubscribeInParallelAsync(brokerUrl, clientCertificate, 1, "Exclusive", true);
    await Task.WhenAll(new[] { taskGeneric, taskTtL, taskBroadcast, taskExclusive });
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error in Felis.Subscriber.Console: '{0}'", ex.Message);
}
finally
{
    Console.WriteLine("Terminated Felis.Subscriber.Console");
}

static async Task SubscribeInParallelAsync(string brokerUrl, X509Certificate2 clientCertificate, int numberOfSubscribers, string topic, bool exclusive)
{
    try
    {
        var subscriberTasks = new Task[numberOfSubscribers];
        for (var i = 0; i < numberOfSubscribers; i++)
        {
            var subscriberId = i + 1;
            subscriberTasks[i] = Task.Run(() => SubscribeAsync(brokerUrl, clientCertificate, subscriberId, topic, exclusive));
        }

        await Task.WhenAll(subscriberTasks);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Error in Felis.Subscriber.Console: '{0}'", ex.Message);
    }
}

static async Task SubscribeAsync(string brokerUrl, X509Certificate2 clientCertificate, int subscriberId, string topic, bool exclusive)
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
        using var response = await client.GetAsync($"{brokerUrl}/{topic}", HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
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
