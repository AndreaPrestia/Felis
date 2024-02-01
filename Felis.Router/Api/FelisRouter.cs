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
          app.MapPost("/dispatch", async ([FromServices] IFelisRouterService service, [FromBody] Message message) =>
            {
                var result = await service.Dispatch(message);

                return !result ? Results.BadRequest("Failed operation") : Results.Created("/dispatch", message);
            }).WithName("MessageDispatch").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

        app.MapPost("/consume",
                async ([FromServices] IFelisRouterService service, [FromBody] ConsumedMessage message) =>
                {
                    var result = await service.Consume(message);

                    return !result ? Results.BadRequest("Failed operation") : Results.Created("/consume", message);
                }).WithName("MessageConsume").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

        app.MapPost("/error", async ([FromServices] IFelisRouterService service, [FromBody] ErrorMessage message) =>
            {
                var result = await service.Error(message);

                return !result ? Results.BadRequest("Failed operation") : Results.Created("/error", message);
            }).WithName("ErrorMessageAdd").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

        app.MapDelete("/purge/{topic}", async ([FromServices] IFelisRouterService service, [FromRoute] string? topic) =>
            {
                var result = await service.Purge(new Topic(topic));

                return !result ? Results.BadRequest("Failed operation") : Results.NoContent();
            }).WithName("MessagePurge").Produces<NoContentResult>(StatusCodes.Status204NoContent)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
        
        app.MapGet("/consumers/{topic}", async ([FromServices] IFelisRouterService service, [FromRoute] string? topic) =>
            {
                var result = await service.Consumers(new Topic(topic));

                return Results.Ok(result);
            }).WithName("ConsumerList").Produces<OkResult>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
    }
}