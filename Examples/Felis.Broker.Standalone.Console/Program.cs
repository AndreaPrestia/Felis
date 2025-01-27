// See https://aka.ms/new-console-template for more information

using Felis;
using Felis.Broker.Standalone.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

try
{
    var cts = new CancellationTokenSource();

    Console.WriteLine("Started Felis.Broker.Standalone.Console");
 
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        Console.WriteLine("Cancel event triggered");
        cts.Cancel();
        eventArgs.Cancel = true;
    };
    
    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        })
        .AddFelisBroker()
        .ConfigureServices((_, services) =>
        {
            services.AddHostedService<Subscriber>();
            services.AddHostedService<Publisher>();
        });

    var host = builder.Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error in Felis.Broker.Standalone.Console {ex.Message}");
}

