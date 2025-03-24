using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Felis;

public static class Extensions
{
    /// <summary>
    /// Adds the Felis broker
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <param name="databasePath"></param>
    /// <returns>IHostBuilder</returns>
    public static IHostBuilder AddBroker(this IHostBuilder hostBuilder, string databasePath)
    {
        return hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddSingleton(p => new MessageBroker(databasePath, p.GetRequiredService<ILogger<MessageBroker>>()));
        });
    }
}