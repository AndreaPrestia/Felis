using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Felis.Endpoints;

internal static class BrokerEndpoints
{
    public static void MapBrokerEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);

        endpointRouteBuilder.MapPost("/{topic}",
            async (HttpContext context,
                [FromServices] MessageBroker messageBroker,
                [FromRoute] string topic,
                [FromHeader(Name = "x-ttl")] int? ttl,
                [FromHeader(Name = "x-broadcast")] bool? broadcast) =>
            {
                ArgumentNullException.ThrowIfNull(context.Request.Body);
                using var reader = new StreamReader(context.Request.Body);
                var payload = await reader.ReadToEndAsync();

                var messageId = messageBroker.Publish(topic, payload, ttl, broadcast);

                return Results.Accepted($"/{topic}", messageId);
            });

        endpointRouteBuilder.MapGet("/{topic}",
            async (HttpContext context, 
                [FromServices] MessageBroker messageBroker, 
                [FromRoute] string topic, 
                [FromHeader(Name = "x-exclusive")] bool? exclusive) =>
            {
                var clientIp = (context.Connection.RemoteIpAddress) ??
                               throw new InvalidOperationException("No Ip address retrieve from Context");

                var clientHostname = (await Dns.GetHostEntryAsync(clientIp)).HostName;

                var subscriptionEntity =
                    messageBroker.Subscribe(topic, clientIp.MapToIPv4().ToString(), clientHostname, exclusive);

                context.Response.Headers.ContentType = "application/x-ndjson";
                context.Response.Headers.CacheControl = "no-cache";
                context.Response.Headers.Connection = "keep-alive";

                var cancellationToken = context.RequestAborted;

                await foreach (var message in subscriptionEntity.MessageChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        messageBroker.UnSubscribe(topic, subscriptionEntity);
                        break;
                    }

                    var messageString = JsonSerializer.Serialize(message);

                    await context.Response.WriteAsync($"{messageString}\n", cancellationToken);
                    await context.Response.Body.FlushAsync(cancellationToken);
                }

                return Results.Empty;
            });
    }
}