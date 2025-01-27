using System.Security.Cryptography.X509Certificates;

try
{
    Console.WriteLine("Started Felis.Subscriber.Console");
    var cts = new CancellationTokenSource();

    Console.CancelKeyPress += (_, eventArgs) =>
    {
        Console.WriteLine("Cancel event triggered");
        cts.Cancel();
        eventArgs.Cancel = true;
    };

    const string pfxPath = "Output.pfx";
    const string pfxPassword = "Password.1";

    var clientCertificate = new X509Certificate2(pfxPath, pfxPassword);
    var brokerUrl = args.FirstOrDefault(a => a.StartsWith("--broker-url="))?.Split("=")[1] ?? "https://localhost:7110";

    var taskGeneric = SubscribeInParallelAsync(brokerUrl, clientCertificate, 20, "Generic", false, cts.Token);
    var taskTtL = SubscribeInParallelAsync(brokerUrl, clientCertificate, 10, "TTL", false, cts.Token);
    var taskBroadcast = SubscribeInParallelAsync(brokerUrl, clientCertificate, 10, "Broadcast", false, cts.Token);
    var taskExclusive = SubscribeInParallelAsync(brokerUrl, clientCertificate, 1, "Exclusive", true, cts.Token);
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

static async Task SubscribeInParallelAsync(string brokerUrl, X509Certificate2 clientCertificate, int numberOfSubscribers, string topic, bool exclusive, CancellationToken cancellationToken)
{
    try
    {
        var subscriberTasks = new Task[numberOfSubscribers];
        for (var i = 0; i < numberOfSubscribers; i++)
        {
            var subscriberId = i + 1;
            subscriberTasks[i] = Task.Run(() => SubscribeAsync(brokerUrl, clientCertificate, subscriberId, topic, exclusive, cancellationToken));
        }

        await Task.WhenAll(subscriberTasks);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Error in Felis.Subscriber.Console: '{0}'", ex.Message);
    }
}

static async Task SubscribeAsync(string brokerUrl, X509Certificate2 clientCertificate, int subscriberId, string topic, bool exclusive, CancellationToken cancellationToken)
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
        using var response = await client.GetAsync($"{brokerUrl}/{topic}", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var content = await reader.ReadLineAsync(cancellationToken);
            Console.WriteLine($"Received message for subscriber {subscriberId} - {topic}: {content}");
        }
    }
    catch (HttpRequestException e)
    {
        Console.WriteLine($"Subscriber {subscriberId} request error: {e.Message}");
    }
}
