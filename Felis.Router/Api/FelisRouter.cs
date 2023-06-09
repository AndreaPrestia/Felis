﻿using Felis.Core;
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

        app.MapGet("/services", async ([FromServices] IFelisRouterService service) =>
            {
                var result = await service.GetConnectedServices();

                return Results.Ok(result);
            }).WithName("ServiceList").Produces<OkResult>()
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
    }
}