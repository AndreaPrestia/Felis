using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using static System.Net.ServicePointManager;

try
{
    Console.WriteLine("Started Felis.Publisher.Net.Console");

    var uri = new Uri("https://username:password@localhost:7110");

    var credentials = Convert.ToBase64String(Encoding.Default.GetBytes(uri.UserInfo));

    var brokerEndpoint = $"{uri.Scheme}://{uri.Authority}";

    using var httpClient = new HttpClient()
    {
        BaseAddress = new Uri(brokerEndpoint)
    };

    SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {credentials}");

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

        response.EnsureSuccessStatusCode();

        await Task.Delay(20);

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

        await Task.Delay(40);

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

        await Task.Delay(4);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error in Felis.Publisher.Net.Console {ex.Message}");
}