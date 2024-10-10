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
        .AddFelisBroker("Output.pfx", "Password.1", 7110, "Felis.db");

    var host = builder.Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error in Felis.Broker.Console {ex.Message}");
}