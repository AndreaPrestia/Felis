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
            async (HttpContext context, [FromServices] MessageBroker messageBroker, [FromRoute] string topic, [FromHeader(Name = "x-retry")] int? retryAttempts) =>
            {
                ArgumentNullException.ThrowIfNull(context.Request.Body);
                using var reader = new StreamReader(context.Request.Body);
                var payload = await reader.ReadToEndAsync();

                var messageId = messageBroker.Publish(topic, payload, retryAttempts);

                return Results.Accepted("/publish", messageId);
            });

        endpointRouteBuilder.MapGet("/{topic}",
            async (HttpContext context, [FromServices] MessageBroker messageBroker, [FromRoute] string topic) =>
            {
                var clientIp = (context.Connection.RemoteIpAddress) ??
                               throw new InvalidOperationException("No Ip address retrieve from Context");

                var clientHostname = (await Dns.GetHostEntryAsync(clientIp)).HostName;

                var subscriptionEntity =
                    messageBroker.Subscribe(clientIp.MapToIPv4().ToString(), clientHostname, topic);

                context.Response.Headers.ContentType = "application/x-ndjson";
                context.Response.Headers.CacheControl = "no-cache";
                context.Response.Headers.Connection = "keep-alive";

                var cancellationToken = context.RequestAborted;

                await foreach (var message in subscriptionEntity.MessageChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        messageBroker.UnSubscribe(subscriptionEntity.Id);
                        break;
                    }

                    var messageString = JsonSerializer.Serialize(message);

                    await context.Response.WriteAsync($"{messageString}\n", cancellationToken);
                    await context.Response.Body.FlushAsync(cancellationToken);

                    messageBroker.Send(message.Id, subscriptionEntity.Subscriber);
                }

                return Results.Empty;
            });

        endpointRouteBuilder.MapGet("/messages/{id}/ack",
            async (HttpContext context, [FromServices] MessageBroker messageBroker, [FromRoute] Guid id) =>
            {
                var clientIp = (context.Connection.RemoteIpAddress) ??
                               throw new InvalidOperationException("No Ip address retrieve from Context");

                var clientHostname = (await Dns.GetHostEntryAsync(clientIp)).HostName;

                messageBroker.Ack(id, clientIp.MapToIPv4().ToString(), clientHostname);

                return Results.NoContent();
            });
    }
}