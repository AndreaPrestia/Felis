using Felis.Core.Models;
using Felis.Router.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Felis.Router.Api;

internal class Router : ApiRouter
{
    public override void Init(WebApplication app)
    {
          app.MapPost("/messages/{topic}/dispatch", async ([FromServices] RouterService service, [FromRoute] string? topic, [FromBody] Message message) =>
            {
                var result = await service.Dispatch(new Topic(topic), message);

                return !result ? Results.BadRequest("Failed operation") : Results.Created("/dispatch", message);
            }).WithName("MessageDispatch").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

        app.MapPost("/messages/{id}/consume",
                async ([FromServices] RouterService service, [FromRoute] Guid id, [FromBody] ConsumedMessage message) =>
                {
                    var result = await service.Consume(id, message);

                    return !result ? Results.BadRequest("Failed operation") : Results.Created("/consume", message);
                }).WithName("MessageConsume").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

        app.MapPost("/messages/{id}/error", async ([FromServices] RouterService service, [FromRoute] Guid id, [FromBody] ErrorMessage message) =>
            {
                var result = await service.Error(id, message);

                return !result ? Results.BadRequest("Failed operation") : Results.Created("/error", message);
            }).WithName("ErrorMessageAdd").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

        app.MapDelete("/messages/{topic}/ready/purge", async ([FromServices] RouterService service, [FromRoute] string? topic) =>
            {
                var result = await service.PurgeReady(new Topic(topic));

                return !result ? Results.BadRequest("Failed operation") : Results.NoContent();
            }).WithName("ReadyMessagePurge").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

        app.MapGet("/messages/{topic}/consumers", async ([FromServices] RouterService service, [FromRoute] string? topic) =>
            {
                var result = await service.Consumers(new Topic(topic));

                return Results.Ok(result);
            }).WithName("ConsumerList").Produces<List<Consumer>>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
        
        app.MapGet("/messages/{topic}/ready", async ([FromServices] RouterService service, [FromRoute] string? topic) =>
            {
                var result = await service.ReadyMessageList(new Topic(topic));

                return Results.Ok(result);
            }).WithName("ReadyMessageList").Produces<List<Message>>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

        app.MapGet("/messages/{topic}/sent", async ([FromServices] RouterService service, [FromRoute] string? topic) =>
            {
                var result = await service.SentMessageList(new Topic(topic));

                return Results.Ok(result);
            }).WithName("SentMessageList").Produces<List<Message>>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);


        app.MapGet("/messages/{topic}/error", async ([FromServices] RouterService service, [FromRoute] string? topic) =>
            {
                var result = await service.ErrorMessageList(new Topic(topic));

                return Results.Ok(result);
            }).WithName("ErrorMessageList").Produces<List<ErrorMessage>>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
              
        app.MapGet("/messages/{topic}/consumed", async ([FromServices] RouterService service, [FromRoute] string? topic) =>
            {
                var result = await service.ConsumedMessageList(new Topic(topic));

                return Results.Ok(result);
            }).WithName("ConsumedMessageList").Produces<List<ConsumedMessage>>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
        
        app.MapGet("/consumers/{connectionId}/messages", async ([FromServices] RouterService service, [FromRoute] string? connectionId) =>
            {
                var result = await service.ConsumedMessageList(new ConnectionId(connectionId));

                return Results.Ok(result);
            }).WithName("ConsumedMessageListByConnectionId").Produces<List<ConsumedMessage>>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
        
        app.MapGet("/consumers/{connectionId}/messages/{topic}", async ([FromServices] RouterService service, [FromRoute] string? connectionId, [FromRoute] string? topic) =>
            {
                var result = await service.ConsumedMessageList(new ConnectionId(connectionId), new Topic(topic));

                return Results.Ok(result);
            }).WithName("ConsumedMessageListByConnectionIdAndTopic").Produces<List<ConsumedMessage>>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
    }
}