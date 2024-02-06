using Felis.Router.Configurations;
using Felis.Router.Hubs;
using Felis.Router.Managers;
using Felis.Router.Services;
using Felis.Router.Services.Background;
using Felis.Router.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            var configuration = context.Configuration.GetSection(FelisRouterConfiguration.FelisRouter).Get<FelisRouterConfiguration>() ?? throw new ApplicationException($"{FelisRouterConfiguration.FelisRouter} configuration not provided");

            if (configuration.MessageConfiguration == null)
            {
                throw new ApplicationException("FelisRouter:MessageConfiguration not provided");
            }

            services.Configure<JsonOptions>(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

            services.AddSignalR();

            AddServices(services);

            AddSwagger(services);
        });
    }

    private static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddHostedService<FelisStorageRequeueService>();
        serviceCollection.AddHostedService<FelisStorageCleanService>();
        serviceCollection.AddSingleton<FelisConnectionManager>();
        serviceCollection.AddSingleton<FelisRouterStorage>();
        serviceCollection.AddSingleton<FelisRouterService>();
        serviceCollection.AddSingleton<FelisRouterHub>();
    }

    private static void AddSwagger(IServiceCollection serviceCollection)
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
                t.IsSubclassOf(typeof(ApiRouter)) && t is { IsInterface: false and false, IsAbstract: false }).ToList().ForEach(t =>
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