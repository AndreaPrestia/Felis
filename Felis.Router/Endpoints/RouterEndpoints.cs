using Felis.Core.Models;
using Felis.Router.Enums;
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
        endpointRouteBuilder.MapPost("/messages/{topic}/dispatch",
              ([FromServices] RouterManager manager, [FromRoute] string? topic,
                   [FromBody] MessageRequest message) =>
              {
                  var result = manager.Dispatch(topic, message);

                  return result == MessageStatus.Error ? Results.BadRequest("Failed operation") : Results.Created("/dispatch", message);
              }).WithName("MessageDispatch").Produces<CreatedResult>(StatusCodes.Status201Created)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapPost("/messages/{id}/consume",
                 ([FromServices] RouterManager manager, [FromRoute] Guid id,
                    [FromBody] ConsumedMessage message) =>
                 {
                     var result = manager.Consume(id, message);

                     return result == MessageStatus.Error ? Results.BadRequest("Failed operation") : Results.NoContent();
                 }).WithName("MessageConsume").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        
        endpointRouteBuilder.MapPost("/messages/{id}/process",
                ([FromServices] RouterManager manager, [FromRoute] Guid id,
                    [FromBody] ProcessedMessage message) =>
                {
                    var result = manager.Process(id, message);

                    return result == MessageStatus.Error ? Results.BadRequest("Failed operation") : Results.NoContent();
                }).WithName("MessageProcess").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapPost("/messages/{id}/error",
                ([FromServices] RouterManager manager, [FromRoute] Guid id,
                    [FromBody] ErrorMessageRequest message) =>
                {
                    var result = manager.Error(id, message);

                    return result == MessageStatus.Error ? Results.BadRequest("Failed operation") : Results.NoContent();
                }).WithName("ErrorMessageAdd").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapDelete("/messages/{topic}/ready/purge",
                ([FromServices] RouterManager manager, [FromRoute] string? topic) =>
                {
                    var result = manager.Purge(topic);

                    return Results.Ok(result);
                }).WithName("ReadyMessagePurge").Produces<Ok<int>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/{topic}/consumers",
                ([FromServices] RouterManager manager, [FromRoute] string topic) =>
                {
                    var result = manager.Consumers(topic);

                    return Results.Ok(result);
                }).WithName("ConsumerList").Produces<List<Consumer>>()
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

        endpointRouteBuilder.MapGet("/consumers/{connectionId}/messages",
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

        endpointRouteBuilder.MapGet("/consumers/{connectionId}/messages/{topic}", ([FromServices] RouterManager manager,
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
    }
}

