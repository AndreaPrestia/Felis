using System.Net;
using System.Text;
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
                async (HttpContext context, [FromServices] MessageBroker messageBroker, [FromRoute] string topic) =>
                {
                    ArgumentNullException.ThrowIfNull(context.Request.Body);
                    using var reader = new StreamReader(context.Request.Body);
                    var payload = await reader.ReadToEndAsync();

                    var messageId = messageBroker.Publish(topic, payload);

                    return Results.Accepted("/publish", messageId);
                }).WithName("Publish").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

        endpointRouteBuilder.MapGet("/{topic}",
            async (HttpContext context, [FromServices] MessageBroker messageBroker, [FromRoute] string topic) =>
            {
                var clientIp = (context.Connection.RemoteIpAddress) ??
                               throw new InvalidOperationException("No Ip address retrieve from Context");

                var clientHostname = (await Dns.GetHostEntryAsync(clientIp)).HostName;

                var subscriberEntity = messageBroker.Subscribe(clientIp.MapToIPv4().ToString(), clientHostname, topic);

                context.Response.Headers.ContentType = "application/octet-stream";
                context.Response.Headers.CacheControl = "no-cache";
                context.Response.Headers.Connection = "keep-alive";
                
                var cancellationToken = context.RequestAborted;

                await foreach (var message in subscriberEntity.MessageChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        messageBroker.UnSubscribe(subscriberEntity.Id);
                        break;
                    }

                    var messageString = JsonSerializer.Serialize(message);
                    var buffer = Encoding.Default.GetBytes(messageString);

                    await context.Response.Body.WriteAsync(buffer, cancellationToken);
                    await context.Response.Body.FlushAsync(cancellationToken);

                    messageBroker.Send(message.Id);
                }

                return Results.Empty;
            }).ExcludeFromDescription();
    }
}