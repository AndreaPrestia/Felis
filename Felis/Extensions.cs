using Felis.Endpoints;
using Felis.Middlewares;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Felis;

public static class Extensions
{
    /// <summary>
    /// Adds the Felis broker with credentials and port
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="certPath">Cert path</param>
    /// <param name="certPassword">Cert password</param>
    /// <param name="port">Port to bind to listen incoming connections</param>
    /// <returns>IHostBuilder</returns>
    public static IHostBuilder AddFelisBroker(this IHostBuilder builder, string certPath, string certPassword, int port)
    {
        return builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseKestrel(options =>
            {
                options.ListenAnyIP(port, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                    listenOptions.UseHttps(new X509Certificate2(certPath, certPassword), httpsOptions =>
                    {
                        httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                        httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                        httpsOptions.AllowAnyClientCertificate();
                        httpsOptions.ClientCertificateValidation = (cert, chain, policyErrors) => true;
                    });
                });
            }).ConfigureServices(services =>
            {
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders =
      ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });

                services.AddCertificateForwarding(
    options => { options.CertificateHeader = "X-ARR-ClientCert"; });

                services.Configure<JsonOptions>(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });

                services.AddRouting();
                services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase("Felis.db"));
                services.AddSingleton<MessageBroker>();
                AddSwagger(services);
            }).Configure(app =>
            {
                app.UseWhen(context => context.Request.Path.ToString().StartsWith("/publish")
                                       || context.Request.Path.ToString().Contains("/subscriber"),
                    appBranch => { appBranch.UseMiddleware<AuthorizationMiddleware>(); });
                app.UseMiddleware<ErrorMiddleware>();

                app.UseRouting();

                app.UseCertificateForwarding();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapBrokerEndpoints();
                    endpoints.MapGet("/", () => "Felis Broker is up and running!").ExcludeFromDescription();
                });

                app.UseSwagger();

                app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json",
                    "Felis Broker v1"));

                app.UseHttpsRedirection();

                app.UseForwardedHeaders();
            });
        });
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