using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

try
{
    Console.WriteLine("Started Felis.Publisher.Net.Console");

    var uri = new Uri("https://localhost:7110");

    var currentDirectory = Path.GetDirectoryName(Directory.GetCurrentDirectory());
    var pfxPath = Path.Combine(currentDirectory!, @"..\..\..\Output.pfx");
    var certificatePath = Path.GetFullPath(pfxPath);
    var clientCertificate = new X509Certificate2(certificatePath, "Password.1");

    using var httpClient = new HttpClient(new HttpClientHandler
    {
        ClientCertificateOptions = ClientCertificateOption.Manual,
        SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
        ServerCertificateCustomValidationCallback = ValidateServerCertificate,
        ClientCertificates = { clientCertificate }
    })
    {
        BaseAddress = uri
    };

    while (true)
    {
        var response = await httpClient.PostAsJsonAsync("/publish",
            new
            {
                Id = Guid.NewGuid(),
                Topic = "Test",
                Payload = JsonSerializer.Serialize(new
                    { Description = $"Test at: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()} from .NET publisher" })
            },
            CancellationToken.None);

        var content = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        await Task.Delay(5000);

        var responseAsync = await httpClient.PostAsJsonAsync("/publish",
            new
            {
                Id = Guid.NewGuid(),
                Topic = "TestAsync",
                Payload = JsonSerializer.Serialize(new
                {
                    Description = $"TestAsync at: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()} from .NET publisher"
                })
            },
            CancellationToken.None);

        responseAsync.EnsureSuccessStatusCode();

        await Task.Delay(5000);

        var responseError = await httpClient.PostAsJsonAsync("/publish",
            new
            {
                Id = Guid.NewGuid(),
                Topic = "TestError",
                Payload = JsonSerializer.Serialize(new
                {
                    Description = $"TestError at: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()} from .NET publisher"
                })
            },
            CancellationToken.None);

        responseError.EnsureSuccessStatusCode();

        await Task.Delay(5000);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error in Felis.Publisher.Net.Console {ex.Message}");
}

static bool ValidateServerCertificate(HttpRequestMessage request, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors errors)
{
    return certificate != null && certificate.Verify() && chain != null;
}