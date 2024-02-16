using Felis.Router.Configurations;
using Felis.Router.Hubs;
using Felis.Router.Managers;
using Felis.Router.Services;
using Felis.Router.Services.Background;
using Felis.Router.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace Felis.Router;

public static class Extensions
{
    public static void AddFelisRouter(this IHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
            services.Configure<FelisRouterConfiguration>(context.Configuration.GetSection(
                FelisRouterConfiguration.FelisRouter));

            var configuration =
                context.Configuration.GetSection(FelisRouterConfiguration.FelisRouter)
                    .Get<FelisRouterConfiguration>() ??
                throw new ApplicationException($"{FelisRouterConfiguration.FelisRouter} configuration not provided");

            if (configuration.MessageConfiguration == null)
            {
                throw new ApplicationException("FelisRouter:MessageConfiguration not provided");
            }

            services.Configure<JsonOptions>(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

            services.AddSignalR();

            services.AddServices();

            if (configuration.LoadBalancingConfiguration != null &&
                !string.IsNullOrWhiteSpace(configuration.LoadBalancingConfiguration.Endpoint))
            {
                services.AddLoadBalancing(configuration);
            }

            services.AddSwagger();
        });
    }

    private static void AddServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHostedService<FelisStorageRequeueService>();
        serviceCollection.AddHostedService<FelisStorageCleanService>();
        serviceCollection.AddHostedService<FelisSenderService>();
        serviceCollection.AddSingleton<FelisConnectionManager>();
        serviceCollection.AddSingleton<FelisRouterStorage>();
        serviceCollection.AddSingleton<FelisRouterService>();
        serviceCollection.AddSingleton<FelisRouterHub>();
        serviceCollection.AddSingleton<FelisLoadBalancingService>();
    }

    private static void AddSwagger(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddEndpointsApiExplorer();

        serviceCollection.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Felis router",
                Description = "Felis router endpoints",
                Contact = new OpenApiContact
                {
                    Name = "Andrea Prestia",
                    Email = "andrea@prestia.dev",
                    Url = new Uri("https://www.linkedin.com/in/andrea-prestia-5212a2166/"),
                }
            });
        });
    }

    private static void AddLoadBalancing(this IServiceCollection serviceCollection,
        FelisRouterConfiguration configuration)
    {
        serviceCollection.AddResponseCompression(opts =>
        {
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                new[] { "application/octet-stream" });
        });

        var hubConnectionBuilder = new HubConnectionBuilder();

        hubConnectionBuilder.Services.AddSingleton<IConnectionFactory>(
            new HttpConnectionFactory(Options.Create(new HttpConnectionOptions()), NullLoggerFactory.Instance));

        serviceCollection.AddSingleton(hubConnectionBuilder
            .WithUrl($"{configuration?.LoadBalancingConfiguration?.Endpoint}/felis/balancer",
                options => { options.Transports = HttpTransportType.WebSockets; })
            .WithAutomaticReconnect()
            .Build());

        var serviceProvider = serviceCollection.BuildServiceProvider();

        var loadBalancingService = serviceProvider.GetService<FelisLoadBalancingService>();

        loadBalancingService?.SubscribeToBalancerAsync().Wait();
    }

    public static void UseFelisRouter(this WebApplication app)
    {
        app.MapHub<FelisRouterHub>("/felis/router");

        app.UseSwagger();

        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json",
            $"Felis Router v1"));

        InitIApiRouterInstances(app);
    }

    private static void InitIApiRouterInstances(WebApplication app)
    {
        AppDomain.CurrentDomain.GetAssemblies().First(x => x.GetName().Name == "Felis.Router")
            .GetTypes().Where(t =>
                t.IsSubclassOf(typeof(ApiRouter)) && t is { IsInterface: false and false, IsAbstract: false }).ToList()
            .ForEach(t =>
            {
                var instance = (ApiRouter)Activator.CreateInstance(t)!;

                if (instance == null!)
                {
                    throw new ApplicationException($"Cannot create an instance of {t.Name}");
                }

                instance.Init(app);
            });
    }
}