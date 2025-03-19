using System.Security.Cryptography.X509Certificates;
using Felis.Http.Endpoints;
using Felis.Http.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Felis.Http;

public static class Extensions
{
    /// <summary>
    /// Adds the http expose for Felis broker with certificate and listening port
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="certificate2">The eventual certificate to use</param>
    /// <param name="port">Port to bind to listen incoming connections. Default value is 7000.</param>
    /// <returns>IHostBuilder</returns>
    public static IHostBuilder WithHttps(this IHostBuilder builder, X509Certificate2? certificate2 = null, int port = 7000)
    {
        return builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseKestrel(options =>
            {
                options.ListenAnyIP(port, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                    if (certificate2 != null)
                    {
                        listenOptions.UseHttps(certificate2);
                    }
                    else
                    {
                        listenOptions.UseHttps();
                    }
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

                services.Configure<JsonOptions>(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });

                services.AddRouting();
            }).Configure(app =>
            {
                app.UseMiddleware<ErrorMiddleware>();

                app.UseRouting();

                app.UseCertificateForwarding();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapBrokerEndpoints();
                    endpoints.MapGet("/", () => "Felis.Http is up and running!");
                });

                app.UseHttpsRedirection();
                app.UseForwardedHeaders();
            });
        });
    }
}