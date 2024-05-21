using Felis.Core.Models;
using Felis.Router.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Felis.Router.Endpoints;

internal static class RouterEndpoints
{
    internal static void MapRouterEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPost("/messages/{topic}/dispatch",
              ([FromServices] RouterService service, [FromRoute] string? topic,
                   [FromBody] Message message) =>
              {
                  var result = service.Dispatch(topic, message);

                  return !result ? Results.BadRequest("Failed operation") : Results.Created("/dispatch", message);
              }).WithName("MessageDispatch").Produces<CreatedResult>(StatusCodes.Status201Created)
           .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
           .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
           .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
           .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
           .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapPost("/messages/{id}/consume",
                 ([FromServices] RouterService service, [FromRoute] Guid id,
                    [FromBody] ConsumedMessage message) =>
                 {
                     var result = service.Consume(id, message);

                     return !result ? Results.BadRequest("Failed operation") : Results.Created("/consume", message);
                 }).WithName("MessageConsume").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapPost("/messages/{id}/error",
                ([FromServices] RouterService service, [FromRoute] Guid id,
                    [FromBody] ErrorMessageRequest message) =>
                {
                    var result = service.Error(id, message);

                    return !result ? Results.BadRequest("Failed operation") : Results.Created("/error", message);
                }).WithName("ErrorMessageAdd").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapDelete("/messages/{topic}/ready/purge",
                ([FromServices] RouterService service, [FromRoute] string? topic) =>
                {
                    var result = service.PurgeReady(topic);

                    return !result ? Results.BadRequest("Failed operation") : Results.NoContent();
                }).WithName("ReadyMessagePurge").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/messages/{topic}/consumers",
                ([FromServices] RouterService service, [FromRoute] string? topic) =>
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
                ([FromServices] RouterService service, [FromRoute] string? topic) =>
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
                ([FromServices] RouterService service, [FromRoute] string? topic) =>
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
                ([FromServices] RouterService service, [FromRoute] string? topic) =>
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
                ([FromServices] RouterService service, [FromRoute] string topic) =>
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
                ([FromServices] RouterService service, [FromRoute] string connectionId) =>
                {
                    var result = service.ConsumedMessageList(connectionId);

                    return Results.Ok(result);
                }).WithName("ConsumedMessageListByConnectionId").Produces<List<ConsumedMessage>>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        endpointRouteBuilder.MapGet("/consumers/{connectionId}/messages/{topic}", ([FromServices] RouterService service,
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

