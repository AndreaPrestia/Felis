using Felis.Core;
using Felis.Router.Hubs;
using Felis.Router.Interfaces;
using Felis.Router.Managers;
using Felis.Router.Services;
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
        builder.Services.Configure<JsonOptions>(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        });
        
        builder.Services.AddSignalR();

		builder.Services.AddSingleton<IFelisConnectionManager, FelisConnectionManager>();
		builder.Services.AddSingleton<IFelisRouterStorage, FelisRouterStorage>();
        builder.Services.AddSingleton<FelisRouterHub>();
        builder.Services.AddSingleton<IFelisRouterService, FelisRouterService>();

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

    //TODO add response result with wrapper
    public static void UseFelisRouter(this WebApplication app)
    {
        app.MapHub<FelisRouterHub>("/felis/router");
        
        app.UseSwagger();
        
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json",
            $"Felis Router v1"));
        
        app.MapPost("/dispatch", async ([FromServices] IFelisRouterService service, [FromBody] Message message) =>
        {
            var result = await service.Dispatch(message);

            return !result ? Results.BadRequest("Failed operation") : Results.Created("/dispatch", message);
        }).WithName("MessageDispatch").Produces<CreatedResult>(StatusCodes.Status201Created)
            .Produces<BadRequestResult>(StatusCodes.Status400BadRequest)
            .Produces<UnauthorizedResult>(StatusCodes.Status401Unauthorized)
            .Produces<ForbidResult>(StatusCodes.Status403Forbidden);
        
        app.MapPost("/consume", async ([FromServices] IFelisRouterService service, [FromBody] ConsumedMessage message) =>
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
    }
}