using Felis.Endpoints;
using Felis.Middlewares;
using Felis.Services;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Felis;

public static class Extensions
{
    /// <summary>
    /// Adds the Felis broker with credentials and port
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="port"></param>
    /// <returns>IHostBuilder</returns>
    public static IHostBuilder AddFelisBroker(this IHostBuilder builder, string username, string password, int port)
    {
        return builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseKestrel(options =>
            {
                options.ListenAnyIP(port, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls13;
                    });
                });
            }).ConfigureServices(services =>
            {
                services.Configure<JsonOptions>(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });

                services.AddRouting();

                services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase("Felis.db"));

                AddServices(services, username, password);

                AddSwagger(services);
            }).Configure(app =>
            {
                app.UseWhen(context => context.Request.Path.ToString().StartsWith("/publish")
                                       || context.Request.Path.ToString().Contains("/subscriber"),
                    appBranch => { appBranch.UseMiddleware<AuthorizationMiddleware>(); });
                app.UseMiddleware<ErrorMiddleware>();

                app.UseRouting();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapBrokerEndpoints();
                    endpoints.MapGet("/", () => "Felis Broker is up and running!").ExcludeFromDescription();
                });

                app.UseSwagger();

                app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json",
                    "Felis Broker v1"));

                app.UseHttpsRedirection();
            });
        });
    }

    private static void AddServices(IServiceCollection serviceCollection, string username, string password)
    {
        serviceCollection.AddSingleton<MessageService>();
        serviceCollection.AddSingleton(_ => new CredentialService(username, password));
        serviceCollection.AddSingleton<MessageBroker>();
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