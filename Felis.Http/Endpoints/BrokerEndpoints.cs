using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Felis.Http.Endpoints;

public static class BrokerEndpoints
{
    public static void MapBrokerEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);

        endpointRouteBuilder.MapPost("/{queue}",
            async (CancellationToken cancellationToken, 
                HttpContext context,
                [FromServices] MessageBroker messageBroker,
                [FromRoute] string queue) =>
            {
                ArgumentNullException.ThrowIfNull(context.Request.Body);

                using var reader = new StreamReader(context.Request.Body);

                var payload = await reader.ReadToEndAsync(cancellationToken);

                var message = await messageBroker.PublishAsync(queue, payload, cancellationToken);

                return Results.Accepted($"/{queue}", message);
            });

        endpointRouteBuilder.MapGet("/{queue}",
            async (ILoggerFactory loggerFactory, HttpContext context,
                [FromServices] MessageBroker messageBroker,
                [FromRoute] string queue,
                [FromHeader(Name = "x-exclusive")] bool exclusive) =>
            {
                var logger = loggerFactory.CreateLogger("Felis");

                var dataStream = context.Response.BodyWriter.AsStream();

                var clientIp = (context.Connection.RemoteIpAddress) ??
                               throw new InvalidOperationException("No Ip address retrieve from Context");

                var clientHostname = (await Dns.GetHostEntryAsync(clientIp)).HostName;

                logger.LogInformation("Subscribed {ipAddress}-{hostname} to queue {queue}",
                    clientIp.MapToIPv4().ToString(), clientHostname, queue);

                context.Response.Headers.ContentType = "application/x-ndjson";
                context.Response.Headers.CacheControl = "no-cache";
                context.Response.Headers.Connection = "keep-alive";

                var cancellationToken = context.RequestAborted;

                try
                {
                    await foreach (var message in messageBroker.Subscribe(queue, exclusive, cancellationToken))
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes($"{JsonSerializer.Serialize(message)}\n");
                        await dataStream.WriteAsync(bytes, cancellationToken);
                        await context.Response.Body.FlushAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Subscriber '{id}' closed connection.", context.Connection.Id);
                }

                return Results.Empty;
            });

        endpointRouteBuilder.MapDelete("/{queue}", async (CancellationToken cancellationToken, [FromServices] MessageBroker messageBroker,
            [FromRoute] string queue) => Results.Ok(await messageBroker.ResetAsync(queue, cancellationToken)));
    }
}