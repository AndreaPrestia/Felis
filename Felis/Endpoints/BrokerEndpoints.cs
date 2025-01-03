using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

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

        endpointRouteBuilder.MapDelete("/{topic}", ([FromServices] MessageBroker messageBroker, [FromRoute] string topic) => Results.Ok(messageBroker.Reset(topic)));

        endpointRouteBuilder.MapGet("/{topic}/{page:int}/{size:int}", ([FromServices] MessageBroker messageBroker, [FromRoute] string topic, [FromRoute] int page, [FromRoute] int size) => Results.Ok(messageBroker.Messages(topic, page, size)));

        endpointRouteBuilder.MapGet("/{topic}",
            async (ILoggerFactory loggerFactory, HttpContext context,
                [FromServices] MessageBroker messageBroker,
                [FromRoute] string topic,
                [FromHeader(Name = "x-exclusive")] bool? exclusive) =>
            {
                var logger = loggerFactory.CreateLogger("Felis");
                var clientIp = (context.Connection.RemoteIpAddress) ??
                               throw new InvalidOperationException("No Ip address retrieve from Context");

                var clientHostname = (await Dns.GetHostEntryAsync(clientIp)).HostName;

                var subscriptionEntity =
                    messageBroker.Subscribe(topic, clientIp.MapToIPv4().ToString(), clientHostname, exclusive);

                context.Response.Headers.ContentType = "application/x-ndjson";
                context.Response.Headers.CacheControl = "no-cache";
                context.Response.Headers.Connection = "keep-alive";

                var dataStream = context.Response.BodyWriter.AsStream();

                if(dataStream == null)
                {
                    logger.LogWarning("Data stream in context null. Subscription {subscriptionId} will be removed.", subscriptionEntity.Id);
                    messageBroker.UnSubscribe(topic, subscriptionEntity);
                    return Results.Empty;
                }

                var cancellationToken = context.RequestAborted;

                try
                {
                    await foreach (var message in subscriptionEntity.MessageChannel.Reader.ReadAllAsync(cancellationToken))
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes($"{JsonSerializer.Serialize(message)}\n");
                        await dataStream.WriteAsync(bytes, cancellationToken);
                        await context.Response.Body.FlushAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Subscriber '{id}' closed connection.", subscriptionEntity.Id);
                }
                finally
                {
                    messageBroker.UnSubscribe(topic, subscriptionEntity);
                }


                return Results.Empty;
            });
    }
}