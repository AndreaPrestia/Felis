﻿// See https://aka.ms/new-console-template for more information

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

try
{
    Console.WriteLine("Started Felis.Subscriber.NoAck.Net.Console");

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

    var request = new HttpRequestMessage(HttpMethod.Get,
        "/Test");
    request.Version = new Version(3, 0);

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
                var jsonMessage = await reader.ReadLineAsync(CancellationToken.None);
                if (!string.IsNullOrWhiteSpace(jsonMessage))
                {
                    try
                    {
                        var messageDeserialized = JsonSerializer.Deserialize<MessageModel>(jsonMessage);

                        var messageFormat =
                            $"Received message - {messageDeserialized?.Id} with topic - {messageDeserialized?.Topic} with payload - {messageDeserialized?.Payload} with expiration - {messageDeserialized?.Expiration}";
                          
                        Console.WriteLine(messageFormat);
                        Console.WriteLine($"I will not send ACK, so the message '{messageDeserialized?.Id}' will be reprocessed other times until the retry policy is consumed.");
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.Message);
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
    Console.Error.WriteLine($"Error in Felis.Subscriber.NoAck.Net.Console {ex.Message}");
}

static bool ValidateServerCertificate(HttpRequestMessage request, X509Certificate2? certificate, X509Chain? chain,
    SslPolicyErrors errors)
{
    return certificate != null && chain != null;
}

public record MessageModel(Guid Id, string Topic, string Payload, long? Expiration);