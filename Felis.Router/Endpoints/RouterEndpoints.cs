using Felis.Core.Models;
using Felis.Router.Services;
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
              ([FromServices] MessageService service, [FromRoute] string? topic,
                   [FromBody] MessageRequest message) =>
              {
                  var result = service.Dispatch(topic, message);

                  return result == Entities.MessageStatus.Error ? Results.BadRequest("Failed operation") : Results.Created("/dispatch", message);
              }).WithName("MessageDispatch").Produces<CreatedResult>(StatusCodes.Status201Created)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapPost("/messages/{id}/consume",
                 ([FromServices] MessageService service, [FromRoute] Guid id,
                    [FromBody] ConsumedMessage message) =>
                 {
                     var result = service.Consume(id, message);

                     return result == Entities.MessageStatus.Error ? Results.BadRequest("Failed operation") : Results.NoContent();
                 }).WithName("MessageConsume").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        
        endpointRouteBuilder.MapPost("/messages/{id}/process",
                ([FromServices] MessageService service, [FromRoute] Guid id,
                    [FromBody] ProcessedMessage message) =>
                {
                    var result = service.Process(id, message);

                    return result == Entities.MessageStatus.Error ? Results.BadRequest("Failed operation") : Results.NoContent();
                }).WithName("MessageProcess").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapPost("/messages/{id}/error",
                ([FromServices] MessageService service, [FromRoute] Guid id,
                    [FromBody] ErrorMessageRequest message) =>
                {
                    var result = service.Error(id, message);

                    return result == Entities.MessageStatus.Error ? Results.BadRequest("Failed operation") : Results.NoContent();
                }).WithName("ErrorMessageAdd").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapDelete("/messages/{topic}/ready/purge",
                ([FromServices] MessageService service, [FromRoute] string? topic) =>
                {
                    var result = service.PurgeReady(topic);

                    return Results.Ok(result);
                }).WithName("ReadyMessagePurge").Produces<Ok<int>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/{topic}/consumers",
                ([FromServices] MessageService service, [FromRoute] string? topic) =>
                {
                    var result = service.Consumers(topic);

                    return Results.Ok(result);
                }).WithName("ConsumerList").Produces<List<Consumer>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/{topic}/ready",
                ([FromServices] MessageService service, [FromRoute] string? topic) =>
                {
                    var result = service.ReadyMessageList(topic);

                    return Results.Ok(result);
                }).WithName("ReadyMessageList").Produces<List<Message>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/{topic}/sent",
                ([FromServices] MessageService service, [FromRoute] string? topic) =>
                {
                    var result = service.SentMessageList(topic);

                    return Results.Ok(result);
                }).WithName("SentMessageList").Produces<List<Message>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/{topic}/error",
                ([FromServices] MessageService service, [FromRoute] string? topic) =>
                {
                    var result = service.ErrorMessageList(topic);

                    return Results.Ok(result);
                }).WithName("ErrorMessageList").Produces<List<ErrorMessage>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/{topic}/consumed",
                ([FromServices] MessageService service, [FromRoute] string topic) =>
                {
                    var result = service.ConsumedMessageList(topic);

                    return Results.Ok(result);
                }).WithName("ConsumedMessageList").Produces<List<ConsumedMessage>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/consumers/{connectionId}/messages",
                ([FromServices] MessageService service, [FromRoute] string connectionId) =>
                {
                    var result = service.ConsumedMessageList(connectionId);

                    return Results.Ok(result);
                }).WithName("ConsumedMessageListByConnectionId").Produces<List<ConsumedMessage>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/consumers/{connectionId}/messages/{topic}", ([FromServices] MessageService service,
                [FromRoute] string connectionId, [FromRoute] string topic) =>
        {
            var result = service.ConsumedMessageList(connectionId, topic);

            return Results.Ok(result);
        }).WithName("ConsumedMessageListByConnectionIdAndTopic").Produces<List<ConsumedMessage>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
    }
}

