using Felis.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Felis.Client;

public static class Extensions
{
    public static void AddFelisClient(this WebApplicationBuilder builder)
    {
        var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(string.IsNullOrEmpty(aspNetCoreEnvironment)
                ? "appsettings.json"
                : $"appsettings.{aspNetCoreEnvironment}.json");
        var config = configurationBuilder.Build();

        var configuration = config.GetSection("FelisClient").Get<FelisConfiguration>();

        if (string.IsNullOrWhiteSpace(configuration?.RouterEndpoint))
        {
            throw new ArgumentNullException(nameof(configuration.RouterEndpoint));
        }

        builder.Services.AddSingleton<FelisConfiguration>(configuration);

        var hubConnectionBuilder = new HubConnectionBuilder();

        hubConnectionBuilder.Services.AddSingleton<IConnectionFactory>(
            new HttpConnectionFactory(Options.Create(new HttpConnectionOptions()), NullLoggerFactory.Instance));

        builder.Services.AddSingleton(hubConnectionBuilder
            .WithUrl($"{configuration.RouterEndpoint}/felis/router",
                options => { options.Transports = HttpTransportType.WebSockets; })
            .WithAutomaticReconnect()
            .Build());

        builder.Services.AddSingleton<MessageHandler>();
    }
}