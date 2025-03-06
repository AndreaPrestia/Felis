using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Felis;

public static class Extensions
{
    private const string DatabasePath = "Felis.db";
    /// <summary>
    /// Adds the Felis broker with heartBeatInSeconds
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <returns>IHostBuilder</returns>
    public static IHostBuilder AddFelisBroker(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices((_, services) =>
        {
            services.AddSingleton(p => new MessageBroker(p.GetRequiredService<ILogger<MessageBroker>>(),
                new LiteDatabase(DatabasePath)));
        });
    }
}