using Felis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

try
{
    Console.WriteLine("Started Felis.Broker.Console");

    var currentDirectory = Path.GetDirectoryName(Directory.GetCurrentDirectory());
    var pfxPath = Path.Combine(currentDirectory!, @"..\..\..\Output.pfx");
    var certificatePath = Path.GetFullPath(pfxPath);

    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        })
        .AddFelisBroker(certificatePath, "Password.1", 7110);

    var host = builder.Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error in Felis.Broker.Console {ex.Message}");
}