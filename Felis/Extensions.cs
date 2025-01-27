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
    /// <param name="heartBeatInSeconds">When the Felis broker should send the heartbeat message to keep connections alive</param>
    /// <returns>IHostBuilder</returns>
    public static IHostBuilder AddFelisBroker(this IHostBuilder hostBuilder, int heartBeatInSeconds = 2)
    {
        return hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddSingleton(_ => new MessageBroker(_.GetRequiredService<ILogger<MessageBroker>>(),
                new LiteDatabase(DatabasePath), heartBeatInSeconds));
        });
    }
}