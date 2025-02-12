using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Felis;
using Felis.Broker.Http.Console;
using Felis.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

try
{
    Console.WriteLine("Started Felis.Broker.Http.Console");
    var cts = new CancellationTokenSource();

    Console.CancelKeyPress += (_, eventArgs) =>
    {
        Console.WriteLine("Cancel event triggered");
        cts.Cancel();
        eventArgs.Cancel = true;
    };
    
    var port = args.FirstOrDefault(a => a.StartsWith("--port="))?.Split("=")[1] ?? "7110";
    var certificateName = args.FirstOrDefault(a => a.StartsWith("--certificate-name="))?.Split("=")[1] ?? "Output.pfx";
    var certificatePassword = args.FirstOrDefault(a => a.StartsWith("--certificate-password="))?.Split("=")[1] ?? "Password.1";

    var certificate = new X509Certificate2(certificateName, certificatePassword);
    
    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        })
        .AddFelisBroker().WithHttp(new X509Certificate2(certificateName, certificatePassword), int.Parse(port))
        .ConfigureServices((_, services) =>
        {
            services.AddHostedService(provider => new Subscriber(provider.GetRequiredService<ILogger<Subscriber>>(), certificate, $"https://localhost:{port}"));
            services.AddHostedService(provider => new Publisher(provider.GetRequiredService<ILogger<Publisher>>(), certificate, $"https://localhost:{port}"));
        });

    var host = builder.Build();

    await host.RunAsync(cts.Token);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error in Felis.Broker.Http.Console {ex.Message}");
}