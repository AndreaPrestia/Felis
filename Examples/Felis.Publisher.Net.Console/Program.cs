using System.Net.Http.Json;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

try
{
    Console.WriteLine("Started Felis.Publisher.Net.Console");

    var uri = new Uri("https://localhost:7110");
    var clientCertificate = new X509Certificate2("Output.pfx", "Password.1");

    using var httpClient = new HttpClient(new HttpClientHandler
    {
        ClientCertificateOptions = ClientCertificateOption.Manual,
        SslProtocols = SslProtocols.Tls13,
        ServerCertificateCustomValidationCallback = ValidateServerCertificate,
        ClientCertificates = { clientCertificate } 
    })
    {
        BaseAddress = uri
    };
    
    httpClient.DefaultRequestHeaders.Add("x-retry", "3");

    while (true)
    {
        var response = await httpClient.PostAsJsonAsync("/Test",
            new
            {
                Description = $"Test at: {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()} from .NET publisher"
            },
            CancellationToken.None);

        response.EnsureSuccessStatusCode();

        await Task.Delay(5000);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error in Felis.Publisher.Net.Console {ex.Message}");
}

static bool ValidateServerCertificate(HttpRequestMessage request, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors errors)
{
    return certificate != null && chain != null;
}