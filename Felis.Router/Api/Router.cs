using Felis.Core.Models;
using Felis.Router.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Felis.Router.Api;

internal class Router : ApiRouter
{
    public override void Init(IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapPost("/messages/{topic}/dispatch",
                   ([FromServices] RouterService service, [FromRoute] string? topic,
                        [FromBody] Message message) =>
                    {
                        var result = service.Dispatch(new Topic(topic), message);

                        return !result ? Results.BadRequest("Failed operation") : Results.Created("/dispatch", message);
                    }).WithName("MessageDispatch").Produces<CreatedResult>(StatusCodes.Status201Created)
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

            endpoints.MapPost("/messages/{id}/consume",
                     ([FromServices] RouterService service, [FromRoute] Guid id,
                        [FromBody] ConsumedMessage message) =>
                    {
                        var result = service.Consume(id, message);

                        return !result ? Results.BadRequest("Failed operation") : Results.Created("/consume", message);
                    }).WithName("MessageConsume").Produces<CreatedResult>(StatusCodes.Status201Created)
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

            endpoints.MapPost("/messages/{id}/error",
                    ([FromServices] RouterService service, [FromRoute] Guid id,
                        [FromBody] ErrorMessageRequest message) =>
                    {
                        var result = service.Error(id, message);

                        return !result ? Results.BadRequest("Failed operation") : Results.Created("/error", message);
                    }).WithName("ErrorMessageAdd").Produces<CreatedResult>(StatusCodes.Status201Created)
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

            endpoints.MapDelete("/messages/{topic}/ready/purge",
                    ([FromServices] RouterService service, [FromRoute] string? topic) =>
                    {
                        var result = service.PurgeReady(new Topic(topic));

                        return !result ? Results.BadRequest("Failed operation") : Results.NoContent();
                    }).WithName("ReadyMessagePurge").Produces<NoContentResult>(StatusCodes.Status204NoContent)
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

            endpoints.MapGet("/messages/{topic}/consumers",
                    ([FromServices] RouterService service, [FromRoute] string? topic) =>
                    {
                        var result = service.Consumers(new Topic(topic));

                        return Results.Ok(result);
                    }).WithName("ConsumerList").Produces<List<Consumer>>()
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

            endpoints.MapGet("/messages/{topic}/ready",
                    ([FromServices] RouterService service, [FromRoute] string? topic) =>
                    {
                        var result = service.ReadyMessageList(new Topic(topic));

                        return Results.Ok(result);
                    }).WithName("ReadyMessageList").Produces<List<Message>>()
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

            endpoints.MapGet("/messages/{topic}/sent",
                    ([FromServices] RouterService service, [FromRoute] string? topic) =>
                    {
                        var result = service.SentMessageList(new Topic(topic));

                        return Results.Ok(result);
                    }).WithName("SentMessageList").Produces<List<Message>>()
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

            endpoints.MapGet("/messages/{topic}/error",
                    ([FromServices] RouterService service, [FromRoute] string? topic) =>
                    {
                        var result = service.ErrorMessageList(new Topic(topic));

                        return Results.Ok(result);
                    }).WithName("ErrorMessageList").Produces<List<ErrorMessage>>()
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

            endpoints.MapGet("/messages/{topic}/consumed",
                    ([FromServices] RouterService service, [FromRoute] string? topic) =>
                    {
                        var result = service.ConsumedMessageList(new Topic(topic));

                        return Results.Ok(result);
                    }).WithName("ConsumedMessageList").Produces<List<ConsumedMessage>>()
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

            endpoints.MapGet("/consumers/{connectionId}/messages",
                    ([FromServices] RouterService service, [FromRoute] string? connectionId) =>
                    {
                        var result = service.ConsumedMessageList(new ConnectionId(connectionId));

                        return Results.Ok(result);
                    }).WithName("ConsumedMessageListByConnectionId").Produces<List<ConsumedMessage>>()
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

            endpoints.MapGet("/consumers/{connectionId}/messages/{topic}", ([FromServices] RouterService service,
                    [FromRoute] string? connectionId, [FromRoute] string? topic) =>
                {
                    var result = service.ConsumedMessageList(new ConnectionId(connectionId), new Topic(topic));

                    return Results.Ok(result);
                }).WithName("ConsumedMessageListByConnectionIdAndTopic").Produces<List<ConsumedMessage>>()
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
        });
    }
}