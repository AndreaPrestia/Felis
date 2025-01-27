using System.Security.Cryptography.X509Certificates;
using Felis;
using Felis.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

try
{
    Console.WriteLine("Started Felis.Broker.Console");
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
 
    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        })
        .AddFelisBroker().WithHttp(new X509Certificate2(certificateName, certificatePassword), int.Parse(port));

    var host = builder.Build();

    await host.RunAsync(cts.Token);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error in Felis.Broker.Console {ex.Message}");
}