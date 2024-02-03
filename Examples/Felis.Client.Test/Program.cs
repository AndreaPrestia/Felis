using Felis.Client;
using Felis.Client.Test.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddFelisClient("https://localhost:7110", 15, 5);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Felis client test",
        Description = "Felis client test endpoints",
        Contact = new OpenApiContact
        {
            Name = "Andrea Prestia",
            Email = "andrea@prestia.dev",
            Url = new Uri("https://www.linkedin.com/in/andrea-prestia-5212a2166/"),
        }
    });
});

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json",
    $"Felis Router v1"));

app.MapGet("/", () => "Felis client is up and running!").ExcludeFromDescription();

app.MapPost("/dispatch", async (MessageHandler messageHandler, [FromBody] TestModel model, [FromQuery] string topic) =>
{
    await messageHandler.PublishAsync(model, topic);
    return Results.Created("/dispatch", model);
});

app.Run();