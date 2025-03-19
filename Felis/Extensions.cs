using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Felis;

public static class Extensions
{
    private const string DatabasePath = "Felis";
    /// <summary>
    /// Adds the Felis broker
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <returns>IHostBuilder</returns>
    public static IHostBuilder AddBroker(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddSingleton(p => new MessageBroker(DatabasePath, p.GetRequiredService<ILogger<MessageBroker>>()));
        });
    }
}