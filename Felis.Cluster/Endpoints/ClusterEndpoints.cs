using Felis.Cluster.Services;
using Felis.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Felis.Cluster.Endpoints
{
    internal static class ClusterEndpoints
    {
        public static void MapFelisClusterEndpoints(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            ArgumentNullException.ThrowIfNull(endpointRouteBuilder);

            endpointRouteBuilder.MapPost("/mirroring/connection/{id}/messages/consume",
                      async (CancellationToken cancellationToken, [FromServices] MirroringService service, [FromRoute] string id,
                        [FromBody] ConsumedMessage message) =>
                     { 
                         await service.ConsumeAsync(new ConnectionId(id), message, cancellationToken).ConfigureAwait(false);

                         return Results.NoContent();
                     }).WithName("MirrorMessageConsume").Produces<NoContentResult>(StatusCodes.Status204NoContent)
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

            endpointRouteBuilder.MapPost("/mirroring/connection/{id}/messages/error",
                    async (CancellationToken cancellationToken, [FromServices] MirroringService service, [FromRoute] string id,
                        [FromBody] ErrorMessage message) =>
                     {
                         await service.ErrorAsync(new ConnectionId(id), message, cancellationToken).ConfigureAwait(false);

                         return Results.NoContent();
                     }).WithName("MirrorMessageError").Produces<NoContentResult>(StatusCodes.Status204NoContent)
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);

            endpointRouteBuilder.MapDelete("/mirroring/connection/{id}/messages/{topic}/ready/purge",
                    async (CancellationToken cancellationToken, [FromServices] MirroringService service, [FromRoute] string id, [FromRoute] string? topic) =>
                     { 
                         await service.PurgeReadyAsync( new ConnectionId(id), new Topic(topic), cancellationToken).ConfigureAwait(false);

                         return Results.NoContent();
                     }).WithName("MirrorReadyMessagePurge").Produces<NoContentResult>(StatusCodes.Status204NoContent)
                .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
                .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
                .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
        }
    }
}
