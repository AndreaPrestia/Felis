using Felis.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Felis.Client;

public static class Extensions
{
    public static void AddFelisClient(this WebApplicationBuilder builder)
    {
        var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            
        var configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(string.IsNullOrEmpty(aspNetCoreEnvironment) ? "appsettings.json" : $"appsettings.{aspNetCoreEnvironment}.json");
        var config = configurationBuilder.Build();

        var configuration = config.GetSection("FelisClient").Get<FelisConfiguration>();
        
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(configuration?.RouterEndpoint ?? throw new ArgumentNullException(nameof(configuration.RouterEndpoint)))
            .Build();
           
        builder.Services.AddSingleton(hubConnection);
        builder.Services.AddSingleton<MessageHandler>();
    }
}