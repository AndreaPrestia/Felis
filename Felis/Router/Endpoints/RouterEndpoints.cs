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

        endpointRouteBuilder.MapPost("/messages/consume",
                ([FromServices] RouterManager manager,
                    [FromBody] ConsumedMessage message) =>
                {
                    var result = manager.Consume(message);

                    return result == MessageStatus.Error ? Results.BadRequest("Failed operation") : Results.NoContent();
                }).WithName("MessageConsume").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapPost("/messages/process",
                ([FromServices] RouterManager manager,
                    [FromBody] ProcessedMessage message) =>
                {
                    var result = manager.Process(message);

                    return result == MessageStatus.Error ? Results.BadRequest("Failed operation") : Results.NoContent();
                }).WithName("MessageProcess").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapPost("/messages/error",
                async (CancellationToken cancellationToken, [FromServices] RouterManager manager,
                    [FromBody] ErrorMessageRequest message) =>
                {
                    var result = await manager.ErrorAsync(message, cancellationToken);

                    return result == MessageStatus.Error ? Results.BadRequest("Failed operation") : Results.NoContent();
                }).WithName("ErrorMessageAdd").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapDelete("/messages/{topic}/ready/purge",
                ([FromServices] RouterManager manager, [FromRoute] string topic) =>
                {
                    var result = manager.Purge(topic);

                    return Results.Ok(result);
                }).WithName("ReadyMessagePurge").Produces<Ok<int>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/{topic}/subscribers",
                ([FromServices] RouterManager manager, [FromRoute] string topic) =>
                {
                    var result = manager.Subscribers(topic);

                    return Results.Ok(result);
                }).WithName("SubscriberList").Produces<List<Common.Models.Subscriber>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/{topic}/ready",
                ([FromServices] RouterManager manager, [FromRoute] string? topic) =>
                {
                    var result = manager.ReadyList(topic);

                    return Results.Ok(result);
                }).WithName("ReadyMessageList").Produces<List<Message>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/{topic}/sent",
                ([FromServices] RouterManager manager, [FromRoute] string? topic) =>
                {
                    var result = manager.SentList(topic);

                    return Results.Ok(result);
                }).WithName("SentMessageList").Produces<List<Message>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/{topic}/error",
                ([FromServices] RouterManager manager, [FromRoute] string? topic) =>
                {
                    var result = manager.ErrorList(topic);

                    return Results.Ok(result);
                }).WithName("ErrorMessageList").Produces<List<ErrorMessage>>()
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

        endpointRouteBuilder.MapGet("/subscribers/{connectionId}/messages/{topic}", (
                [FromServices] RouterManager manager,
                [FromRoute] string connectionId, [FromRoute] string topic) =>
            {
                var result = manager.ConsumedList(connectionId, topic);

                return Results.Ok(result);
            }).WithName("ConsumedMessageListByConnectionIdAndTopic").Produces<List<ConsumedMessage>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapPost("/messages/enqueue",
                ([FromServices] RouterManager manager, [FromBody] MessageRequest message) =>
                {
                    var result = manager.Enqueue(message);

                    return result == MessageStatus.Error
                        ? Results.BadRequest("Failed operation")
                        : Results.Created("/dispatch", message);
                }).WithName("Enqueue").Produces<CreatedResult>(StatusCodes.Status201Created)
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
                }).WithName("Ack").Produces<NoContentResult>(StatusCodes.Status204NoContent)
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
                }).WithName("Nack").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}