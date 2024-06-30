using Felis.Common.Models;
using Felis.Router.Managers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Felis.Router.Endpoints;

internal static class RouterEndpoints
{
    internal static void MapRouterEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("/messages/dispatch",
                async (CancellationToken cancellationToken, [FromServices] RouterManager manager,
                    [FromRoute] string topic,
                    [FromBody] MessageRequest message) =>
                {
                    var result = await manager.DispatchAsync(message, cancellationToken);

                    return result == MessageStatus.Error
                        ? Results.BadRequest("Failed operation")
                        : Results.Created("/dispatch", message);
                }).WithName("MessageDispatch").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapPost("/messages/publish",
                ([FromServices] RouterManager manager, [FromBody] MessageRequest message) =>
                {
                    var result = manager.Publish(message);

                    return result == MessageStatus.Error
                        ? Results.BadRequest("Failed operation")
                        : Results.Created("/publish", message);
                }).WithName("MessagePublish").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapPost("/messages/ack",
                ([FromServices] RouterManager manager, [FromBody] ConsumedMessage message) =>
                {
                    var result = manager.Ack(message);

                    return result == MessageStatus.Error ? Results.BadRequest("Failed operation") : Results.NoContent();
                }).WithName("MessageAck").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapPost("/messages/nack",
                ([FromServices] RouterManager manager, [FromBody] ErrorMessageRequest message) =>
                {
                    var result = manager.Nack(message);

                    return result == MessageStatus.Error ? Results.BadRequest("Failed operation") : Results.NoContent();
                }).WithName("MessageNack").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapDelete("/messages/topics/{topic}/ready/purge",
                ([FromServices] RouterManager manager, [FromRoute] string topic) =>
                {
                    var result = manager.PurgeByTopic(topic);

                    return Results.Ok(result);
                }).WithName("ReadyMessagePurgeByTopic").Produces<Ok<int>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapDelete("/messages/queues/{queue}/ready/purge",
                ([FromServices] RouterManager manager, [FromRoute] string queue) =>
                {
                    var result = manager.PurgeByQueue(queue);

                    return Results.Ok(result);
                }).WithName("ReadyMessagePurgeByQueue").Produces<Ok<int>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/topics/{topic}/subscribers",
                ([FromServices] RouterManager manager, [FromRoute] string topic) =>
                {
                    var result = manager.SubscribersByTopic(topic);

                    return Results.Ok(result);
                }).WithName("SubscriberListByTopic").Produces<List<Common.Models.Subscriber>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/queues/{queue}/subscribers",
                ([FromServices] RouterManager manager, [FromRoute] string queue) =>
                {
                    var result = manager.SubscribersByQueue(queue);

                    return Results.Ok(result);
                }).WithName("SubscriberListByQueue").Produces<List<Common.Models.Subscriber>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/topics/{topic}/ready",
                ([FromServices] RouterManager manager, [FromRoute] string topic) =>
                {
                    var result = manager.ReadyListByTopic(topic);

                    return Results.Ok(result);
                }).WithName("ReadyMessageListByTopic").Produces<List<Message>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/queues/{queue}/ready",
                ([FromServices] RouterManager manager, [FromRoute] string queue) =>
                {
                    var result = manager.ReadyListByQueue(queue);

                    return Results.Ok(result);
                }).WithName("ReadyMessageListByQueue").Produces<List<Message>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/topics/{topic}/sent",
                ([FromServices] RouterManager manager, [FromRoute] string topic) =>
                {
                    var result = manager.SentListByTopic(topic);

                    return Results.Ok(result);
                }).WithName("SentMessageListByTopic").Produces<List<Message>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/queues/{queue}/sent",
                ([FromServices] RouterManager manager, [FromRoute] string queue) =>
                {
                    var result = manager.SentListByQueue(queue);

                    return Results.Ok(result);
                }).WithName("SentMessageListByQueue").Produces<List<Message>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/topics/{topic}/error",
                ([FromServices] RouterManager manager, [FromRoute] string topic) =>
                {
                    var result = manager.ErrorListByTopic(topic);

                    return Results.Ok(result);
                }).WithName("ErrorMessageListByTopic").Produces<List<ErrorMessage>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/queues/{queue}/error",
                ([FromServices] RouterManager manager, [FromRoute] string queue) =>
                {
                    var result = manager.ErrorListByQueue(queue);

                    return Results.Ok(result);
                }).WithName("ErrorMessageListByQueue").Produces<List<ErrorMessage>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/{topic}/consumed",
                ([FromServices] RouterManager manager, [FromRoute] string topic) =>
                {
                    var result = manager.ConsumedMessageList(topic);

                    return Results.Ok(result);
                }).WithName("ConsumedMessageList").Produces<List<ConsumedMessage>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/subscribers/{connectionId}/messages",
                ([FromServices] RouterManager manager, [FromRoute] string connectionId) =>
                {
                    var result = manager.ConsumedListByConnectionId(connectionId);

                    return Results.Ok(result);
                }).WithName("ConsumedMessageListByConnectionId").Produces<List<ConsumedMessage>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/subscribers/{connectionId}/messages/topics/{topic}", (
                [FromServices] RouterManager manager,
                [FromRoute] string connectionId, [FromRoute] string topic) =>
            {
                var result = manager.ConsumedListByTopic(connectionId, topic);

                return Results.Ok(result);
            }).WithName("ConsumedMessageListByConnectionIdAndTopic").Produces<List<ConsumedMessage>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/subscribers/{connectionId}/messages/queues/{queue}", (
                [FromServices] RouterManager manager,
                [FromRoute] string connectionId, [FromRoute] string queue) =>
            {
                var result = manager.ConsumedListByQueue(connectionId, queue);

                return Results.Ok(result);
            }).WithName("ConsumedMessageListByConnectionIdAndQueue").Produces<List<ConsumedMessage>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}