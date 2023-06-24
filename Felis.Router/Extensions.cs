using Felis.Router.Configurations;
using Felis.Router.Hubs;
using Felis.Router.Interfaces;
using Felis.Router.Managers;
using Felis.Router.Services;
using Felis.Router.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Felis.Router;

public static class Extensions
{
    public static void AddFelisRouter(this WebApplicationBuilder builder)
    {
        var configuration = GetFelisRouterConfiguration(builder);

        builder.Services.Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });

        builder.Services.AddSignalR();

        AddServices(builder, configuration);

        AddSwagger(builder);
    }

    private static FelisRouterConfiguration GetFelisRouterConfiguration(WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration.GetSection("FelisRouter").Get<FelisRouterConfiguration>();

        if (configuration == null)
        {
            throw new ApplicationException("FelisRouter configuration not provided");
        }

        if (configuration.MessageConfiguration == null)
        {
            throw new ApplicationException("FelisRouter:MessageConfiguration not provided");
        }

        if (configuration.StorageConfiguration == null)
        {
            throw new ApplicationException("FelisRouter:StorageConfiguration not provided");
        }

        return configuration;
    }

    private static void AddServices(WebApplicationBuilder builder, FelisRouterConfiguration? configuration)
    {
        builder.Services.AddSingleton(configuration!);

        builder.Services.AddSingleton<IFelisConnectionManager, FelisConnectionManager>();

        if (string.Equals(KnownStorageStrategies.Persistent, configuration?.StorageConfiguration?.Strategy))
        {
            var concreteType = GetStorageSourceConcreteTypeNotInMemory();

            if (concreteType == null)
            {
                throw new ApplicationException(
                    $"No concrete type different than FelisRouterStorage implemented for StorageConfiguration:Strategy {KnownStorageStrategies.Persistent}.");
            }
            
            //TODO review this terrible code!
            builder.Services.AddSingleton<IFelisRouterStorage>((IFelisRouterStorage)concreteType);
        }
        else
        {
            builder.Services.AddSingleton<IFelisRouterStorage, FelisRouterStorage>();
        }

        builder.Services.AddSingleton<IFelisRouterService, FelisRouterService>();
        builder.Services.AddSingleton<FelisRouterHub>();
    }

    private static void AddSwagger(WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Felis router",
                Description = "Felis router endpoints",
                Contact = new OpenApiContact
                {
                    Name = "Andrea Prestia",
                    Email = string.Empty,
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
                t.IsSubclassOf(typeof(ApiRouter)) && !t.IsInterface
                                                  && !t.IsAbstract && !t.IsInterface).ToList().ForEach(t =>
            {
                var instance = (ApiRouter)Activator.CreateInstance(t)!;

                if (instance == null!)
                {
                    throw new ApplicationException($"Cannot create an instance of {t.Name}");
                }

                instance.Init(app);
            });
    }

    private static Type? GetStorageSourceConcreteTypeNotInMemory()
    {
       return AppDomain.CurrentDomain.GetAssemblies().First(x => x.GetName().Name == AppDomain.CurrentDomain.FriendlyName)
            .GetTypes().FirstOrDefault(t =>
                t.IsSubclassOf(typeof(IFelisRouterStorage)) && !t.IsInterface
                                                            && !t.IsAbstract && t != typeof(FelisRouterStorage));
    }
}