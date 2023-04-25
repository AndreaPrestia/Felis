using Felis.Core;
using Felis.Router.Hubs;
using Felis.Router.Interfaces;
using Felis.Router.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Felis.Router;

public static class Extensions
{
    public static void AddFelisRouter(this WebApplicationBuilder builder)
    {
        builder.Services.AddSignalR();

        builder.Services.AddSingleton<IFelisRouterStorage, FelisRouterStorage>();
        builder.Services.AddSingleton<IFelisRouterHub, FelisRouterHub>();

        builder.Services.AddEndpointsApiExplorer();
        
        builder.Services.AddSwaggerGen(c =>  
        {  
            c.SwaggerDoc("v1", new OpenApiInfo  
            {  
                Version = "v1",  
                Title = "Felis router",  
                Description = "Felis router endpoints",  
                Contact = new OpenApiContact  
                {  
                    Name = "Andrea Prestia",  
                    Email = string.Empty,  
                    Url = new Uri("https://www.linkedin.com/in/andrea-prestia-5212a2166/"),  
                }
            });  
        });  
    }

    public static void UseFelisRouter(this WebApplication app)
    {
        app.MapHub<FelisRouterHub>("/felis/router");
        
        app.UseSwagger();
        
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json",
            $"Felis Router v1"));
        
        app.MapPost("/dispatch", async ([FromServices] IFelisRouterHub felisRouterHub, [FromBody] Message message) =>
        {
            var result = await felisRouterHub.Dispatch(message);

            return !result ? Results.BadRequest("Failed operation") : Results.Created("/dispatch", message);
        }).WithName("MessageDispatch").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
        
        app.MapPost("/consume", async ([FromServices] IFelisRouterHub felisRouterHub, [FromBody] ConsumedMessage message) =>
        {
            var result = await felisRouterHub.Consume(message);

            return !result ? Results.BadRequest("Failed operation") : Results.Created("/consume", message);
        }).WithName("MessageConsume").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
    }
}