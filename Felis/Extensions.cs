using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Felis;

public static class Extensions
{
    /// <summary>
    /// Adds the Felis broker with options configuration
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <param name="configure">Action to configure broker options</param>
    /// <returns>IHostBuilder</returns>
    public static IHostBuilder AddBroker(this IHostBuilder hostBuilder, Action<MessageBrokerOptions> configure)
    {
        var options = new MessageBrokerOptions();
        configure(options);
        return hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddSingleton(p => new MessageBroker(options, p.GetRequiredService<ILogger<MessageBroker>>()));
        });
    }

    /// <summary>
    /// Adds the Felis broker with a database path
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <param name="databasePath"></param>
    /// <returns>IHostBuilder</returns>
    public static IHostBuilder AddBroker(this IHostBuilder hostBuilder, string databasePath)
    {
        return hostBuilder.AddBroker(options => options.DataPath = databasePath);
    }
}