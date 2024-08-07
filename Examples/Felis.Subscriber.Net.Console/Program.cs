// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Text;
using System.Text.Json;
using static System.Net.ServicePointManager;

try
{
    Console.WriteLine("Started Felis.Subscriber.Net.Console");
    
    var uri = new Uri("https://username:password@localhost:7110");

    var credentials = Convert.ToBase64String(Encoding.Default.GetBytes(uri.UserInfo));

    var brokerEndpoint = $"{uri.Scheme}://{uri.Authority}";

    using var httpClient = new HttpClient()
    {
        BaseAddress = new Uri(brokerEndpoint)
    };

    SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {credentials}");

    var request = new HttpRequestMessage(HttpMethod.Get,
        $"/subscribe?topics=Test,TestAsync,TestError");
    request.Version = new Version(2, 0);

    using var response =
        await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, CancellationToken.None);

    if (response.IsSuccessStatusCode)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(CancellationToken.None);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            try
            {
                var message = await reader.ReadLineAsync(CancellationToken.None);
                if (!string.IsNullOrWhiteSpace(message) && message.StartsWith("data:"))
                {
                    var jsonMessage = message.Split("data:").LastOrDefault();

                    if (!string.IsNullOrWhiteSpace(jsonMessage))
                    {
                        try
                        {
                            var messageDeserialized = JsonSerializer.Deserialize<MessageModel>(jsonMessage);

                            var messageFormat =
                                $"Received message - {messageDeserialized?.Id} with topic - {messageDeserialized?.Topic} with payload - {messageDeserialized?.Payload}";
                            if (string.Equals("Test", messageDeserialized?.Topic,
                                    StringComparison.InvariantCultureIgnoreCase))
                            {
                                Console.WriteLine(messageFormat);
                            }
                            else if (string.Equals("TestAsync", messageDeserialized?.Topic,
                                         StringComparison.InvariantCultureIgnoreCase))
                            {
                                await Task.Run(() =>
                                    Console.WriteLine(messageFormat));
                            }
                            else
                            {
                                throw new Exception(messageFormat);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
    else
    {
        Console.Error.WriteLine($"Error: {response.StatusCode}");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error in Felis.Subscriber.Net.Console {ex.Message}");
}

public record MessageModel(Guid Id, string Topic, string Payload);