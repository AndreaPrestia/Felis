using Felis.Core;
using Felis.Core.Models;
using Felis.Router.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Felis.Router.Api;

internal class FelisRouter : ApiRouter
{
    public override void Init(WebApplication app)
    {
          app.MapPost("/messages/{topic}/dispatch", async ([FromServices] IFelisRouterService service, [FromRoute] string? topic, [FromBody] Message message) =>
            {
                var result = await service.Dispatch(new Topic(topic), message).ConfigureAwait(false);

                return !result ? Results.BadRequest("Failed operation") : Results.Created("/dispatch", message);
            }).WithName("MessageDispatch").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

        app.MapPost("/messages/{id}/consume",
                async ([FromServices] IFelisRouterService service, [FromRoute] Guid id, [FromBody] ConsumedMessage message) =>
                {
                    var result = await service.Consume(id, message).ConfigureAwait(false);

                    return !result ? Results.BadRequest("Failed operation") : Results.Created("/consume", message);
                }).WithName("MessageConsume").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

        app.MapPost("/messages/{id}/error", async ([FromServices] IFelisRouterService service, [FromRoute] Guid id, [FromBody] ErrorMessage message) =>
            {
                var result = await service.Error(id, message).ConfigureAwait(false);

                return !result ? Results.BadRequest("Failed operation") : Results.Created("/error", message);
            }).WithName("ErrorMessageAdd").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

        app.MapDelete("/messages/{topic}/purge", async ([FromServices] IFelisRouterService service, [FromRoute] string? topic) =>
            {
                var result = await service.Purge(new Topic(topic)).ConfigureAwait(false);

                return !result ? Results.BadRequest("Failed operation") : Results.NoContent();
            }).WithName("MessagePurge").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
        
        app.MapGet("/messages/{topic}/consumers", async ([FromServices] IFelisRouterService service, [FromRoute] string? topic) =>
            {
                var result = await service.Consumers(new Topic(topic)).ConfigureAwait(false);

                return Results.Ok(result);
            }).WithName("ConsumerList").Produces<List<Service>>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
        
        app.MapGet("/messages/{topic}", async ([FromServices] IFelisRouterService service, [FromRoute] string? topic) =>
            {
                var result = await service.MessageList(new Topic(topic)).ConfigureAwait(false);

                return Results.Ok(result);
            }).WithName("MessageList").Produces<List<Message>>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
        
        app.MapGet("/messages/{topic}/error", async ([FromServices] IFelisRouterService service, [FromRoute] string? topic) =>
            {
                var result = await service.ErrorMessageList(new Topic(topic)).ConfigureAwait(false);

                return Results.Ok(result);
            }).WithName("ErrorMessageList").Produces<List<ErrorMessage>>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
              
        app.MapGet("/messages/{topic}/consumed", async ([FromServices] IFelisRouterService service, [FromRoute] string? topic) =>
            {
                var result = await service.ConsumedMessageList(new Topic(topic)).ConfigureAwait(false);

                return Results.Ok(result);
            }).WithName("ConsumedMessageList").Produces<List<ConsumedMessage>>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
        
        app.MapGet("/consumers/{connectionId}/messages", async ([FromServices] IFelisRouterService service, [FromRoute] string? connectionId) =>
            {
                var result = await service.ConsumedMessageList(new ConnectionId(connectionId)).ConfigureAwait(false);

                return Results.Ok(result);
            }).WithName("ConsumedMessageListByConnectionId").Produces<List<ConsumedMessage>>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
        
        app.MapGet("/consumers/{connectionId}/messages/{topic}", async ([FromServices] IFelisRouterService service, [FromRoute] string? connectionId, [FromRoute] string? topic) =>
            {
                var result = await service.ConsumedMessageList(new ConnectionId(connectionId), new Topic(topic)).ConfigureAwait(false);

                return Results.Ok(result);
            }).WithName("ConsumedMessageListByConnectionIdAndTopic").Produces<List<ConsumedMessage>>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
    }
}