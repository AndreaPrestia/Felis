using Felis.Router.Endpoints;
using Felis.Router.Hubs;
using Felis.Router.Managers;
using Felis.Router.Middlewares;
using Felis.Router.Services;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Felis.Router;

public static class Extensions
{
    public static void UseFelisRouter(this IApplicationBuilder app)
    {
        app.UseWhen(context => context.Request.Path.ToString().StartsWith("/messages")
                               || context.Request.Path.ToString().Contains("/consumers")
                               || context.Request.Path.ToString().Contains("/felis/router"),
            appBranch => { appBranch.UseMiddleware<AuthorizationMiddleware>(); });
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

    public static void AddFelisRouter(this IServiceCollection services, string username, string password)
    {
        services.Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });

        services.AddSignalR();

        AddServices(services);

        services.AddSingleton<CredentialService>(_ => new CredentialService(username, password));

        AddSwagger(services);

        services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase("Felis.db"));

        services.AddSingleton(_ => new RouterManager(
            _.GetRequiredService<ILogger<RouterManager>>(),
            _.GetRequiredService<MessageService>(),
            _.GetRequiredService<ConnectionService>(),
            _.GetRequiredService<DeadLetterService>(),
            _.GetRequiredService<IHubContext<RouterHub>>()
        ));
    }

    private static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<RouterHub>();
        serviceCollection.AddSingleton<ConnectionService>();
        serviceCollection.AddSingleton<MessageService>();
        serviceCollection.AddSingleton<DeadLetterService>();
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