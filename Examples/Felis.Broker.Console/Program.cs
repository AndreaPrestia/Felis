using System.Security.Cryptography.X509Certificates;
using Felis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

try
{
    Console.WriteLine("Started Felis.Broker.Console");

    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        })
        .AddFelisBroker(new X509Certificate2("Output.pfx", "Password.1"));

    var host = builder.Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error in Felis.Broker.Console {ex.Message}");
}