using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.Server;

namespace Felis.Mqtt;

public static class HostBuilderExtensions
{
    public static IHostBuilder WithMqtt(this IHostBuilder hostBuilder, int port, X509Certificate2 certificate)
    {
        return hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddSingleton<MqttServer>(_ =>
            {
                var optionsBuilder = new MqttServerOptionsBuilder()
                    .WithDefaultEndpointPort(port)
                    .WithKeepAlive()
                    .WithoutDefaultEndpoint()
                    .WithEncryptedEndpoint()
                    .WithEncryptedEndpointPort(port)
                    .WithEncryptionCertificate(certificate.Export(X509ContentType.Pfx))
                    .WithEncryptionSslProtocol(SslProtocols.Tls13);

                var options = optionsBuilder.Build();
                var mqttServer = new MqttServerFactory().CreateMqttServer(options);
                mqttServer.StartAsync().GetAwaiter().GetResult();

                return mqttServer;
            });

            services.AddHostedService<MqttServerLifetimeService>();
        });
    }
}