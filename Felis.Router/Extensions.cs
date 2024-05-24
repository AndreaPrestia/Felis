using Felis.Router.Abstractions;
using Felis.Router.Endpoints;
using Felis.Router.Hubs;
using Felis.Router.Managers;
using Felis.Router.Middlewares;
using Felis.Router.Services;
using Felis.Router.Services.Background;
using Felis.Router.Storage;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Felis.Router;

public static class Extensions
{
    public static void UseFelisRouter(this IApplicationBuilder app)
    {
        app.UseMiddleware<ErrorMiddleware>();

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<RouterHub>("/felis/router");
            endpoints.MapRouterEndpoints();
            endpoints.MapGet("/", () => "Felis Router is up and running!").ExcludeFromDescription();
        });

        app.UseSwagger();

        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json",
            "Felis Router v1"));
    }

    public static void AddFelisRouter(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });

        services.AddSignalR();

        AddServices(services);

        AddSwagger(services);
        services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase("Felis.db"));
        services.AddSingleton<IRouterStorage, LiteDbRouterStorage>();
    }

    private static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddHostedService<SenderService>();
        serviceCollection.AddSingleton<ConnectionManager>();
        serviceCollection.AddSingleton<RouterService>();
        serviceCollection.AddSingleton<RouterHub>();
        serviceCollection.AddSingleton<LoadBalancingService>();
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
}