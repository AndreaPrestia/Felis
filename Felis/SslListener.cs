using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Felis;

internal class SslListener
{
    private readonly int _port;
    private readonly int _backlog;
    private readonly X509Certificate2 _serverCertificate;
    private readonly ConcurrentDictionary<SubscriptionModel, SslStream> _clients = new();
    private readonly ILogger<SslListener> _logger;
    private readonly MessageBroker _messageBroker;

    public SslListener(int port, string certificatePath, string certificatePassword, int backlog,
        ILogger<SslListener> logger, MessageBroker messageBroker)
    {
        _port = port;
        _backlog = backlog;
        _serverCertificate = new X509Certificate2(certificatePath, certificatePassword);
        _logger = logger;
        _messageBroker = messageBroker;
    }

    public async Task StartAsync()
    {
        var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Any, _port));
        listener.Listen(_backlog);
        _logger.LogInformation("Server listening on port {port}.", _port);

        while (true)
        {
            var tcpClient = await listener.AcceptAsync();
            _ = HandleClientAsync(tcpClient); // Fire-and-forget to handle connections concurrently
        }
    }

    private async Task HandleClientAsync(Socket socket)
    {
        var clientId = Guid.NewGuid().ToString();
        _clients[clientId] = socket;

        _logger.LogDebug($"Client connected: {clientId}");

        try
        {
            await using var networkStream = new NetworkStream(socket, ownsSocket: true);
            await using var sslStream = new SslStream(networkStream, false);
            await sslStream.AuthenticateAsServerAsync(_serverCertificate, clientCertificateRequired: false,
                checkCertificateRevocation: true);

            if (!sslStream.IsAuthenticated)
            {
                _logger.LogWarning($"Client {clientId} failed SSL authentication.");
                return;
            }

            _logger.LogDebug($"Client {clientId} authenticated.");

            var buffer = new byte[1024];
            while (socket.Connected)
            {
                var bytesRead = await sslStream.ReadAsync(buffer);
                if (bytesRead == 0)
                {
                    //TODO check
                    break; // Client disconnected
                }

                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                _logger.LogDebug($"Received from {clientId}: {receivedMessage}");

                var splittedRequest = receivedMessage.Split('/');

                var topic = splittedRequest.FirstOrDefault();
                var exclusive = splittedRequest.LastOrDefault();

                if (string.IsNullOrWhiteSpace(topic))
                {
                    break;
                }

                var parametersDictionary = new Dictionary<string, string>();

                var subscriptionEntity =
                    _messageBroker.Subscribe(topic, string.Empty, string.Empty, !string.IsNullOrWhiteSpace(exclusive));

                _clients.TryAdd(subscriptionEntity, sslStream);
                // Echo the message back
                var response = Encoding.UTF8.GetBytes($"Echo: {receivedMessage}");
                await sslStream.WriteAsync(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error with client {clientId}: {ex.Message}");
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            socket.Dispose();
            _logger.LogInformation($"Client {clientId} disconnected.");
        }
    }

    public Task StopAsync()
    {
        _logger.LogInformation("Stopping server...");
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }
        _logger.LogInformation("Server stopped.");

        return Task.CompletedTask;
    }
    
    private void FireAndForget(Task task)
    {
        task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                _logger.LogWarning($"Task failed: {t.Exception?.GetBaseException().Message}");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}