using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Felis.Http.Endpoints;
using Felis.Http.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Felis.Http;

public static class Extensions
{
    /// <summary>
    /// Adds the Felis broker with certificate path, password and listening port
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="certificate">The certificate to use</param>
    /// <param name="port">Port to bind to listen incoming connections. Default value is 7000.</param>
    /// <param name="certificateForwardingHeader">Header to use for certificate forwarding when Felis is under a proxy. Default value is 'X-ARR-ClientCert'</param>
    /// <returns>IHostBuilder</returns>
    public static IHostBuilder WithHttp(this IHostBuilder builder, X509Certificate2 certificate, int port = 7000, string certificateForwardingHeader = "X-ARR-ClientCert")
    {
        return builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseKestrel(options =>
            {
                options.ListenAnyIP(port, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                    listenOptions.UseHttps(certificate, httpsOptions =>
                    {
                        httpsOptions.SslProtocols = SslProtocols.Tls13;
                        httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                        httpsOptions.AllowAnyClientCertificate();
                        httpsOptions.ClientCertificateValidation = (cert, _, _) => cert.Thumbprint == certificate.Thumbprint && cert.Issuer == certificate.Issuer;
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
                    options =>
                    {
                        options.CertificateHeader = certificateForwardingHeader;

                        if (string.Equals("ssl-client-cert", certificateForwardingHeader))
                        {
                            options.HeaderConverter = (headerValue) =>
                            {
                                X509Certificate2? clientCertificate = null;

                                if (!string.IsNullOrWhiteSpace(headerValue))
                                {
                                    clientCertificate = X509Certificate2.CreateFromPem(
                                        WebUtility.UrlDecode(headerValue));
                                }

                                return clientCertificate!;
                            };
                        }
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