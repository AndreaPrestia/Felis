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

        endpointRouteBuilder.MapPost("/{topic}",
            async (HttpContext context,
                [FromServices] MessageBroker messageBroker,
                [FromRoute] string topic) =>
            {
                ArgumentNullException.ThrowIfNull(context.Request.Body);
                
                using var reader = new StreamReader(context.Request.Body);
                
                var payload = await reader.ReadToEndAsync();

                var messageId = messageBroker.Publish(topic, payload);

                return Results.Accepted($"/{topic}", messageId);
            });
        
        endpointRouteBuilder.MapGet("/{topic}",
            async (ILoggerFactory loggerFactory, HttpContext context,
                [FromServices] MessageBroker messageBroker,
                [FromRoute] string topic,
                [FromHeader(Name = "x-exclusive")] bool exclusive) =>
            {
                var logger = loggerFactory.CreateLogger("Felis");

                var dataStream = context.Response.BodyWriter.AsStream();
                
                var clientIp = (context.Connection.RemoteIpAddress) ?? throw new InvalidOperationException("No Ip address retrieve from Context");

                var clientHostname = (await Dns.GetHostEntryAsync(clientIp)).HostName;

                logger.LogInformation("Subscribed {ipAddress}-{hostname} to topic {topic}", clientIp.MapToIPv4().ToString(), clientHostname, topic);

                context.Response.Headers.ContentType = "application/x-ndjson";
                context.Response.Headers.CacheControl = "no-cache";
                context.Response.Headers.Connection = "keep-alive";

                var cancellationToken = context.RequestAborted;

                try
                {
                    await foreach (var message in messageBroker.Subscribe(topic, exclusive, cancellationToken))
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
    }
}