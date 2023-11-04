using Felis.Client;
using Felis.Client.Test.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddFelisClient();

var app = builder.Build();

app.MapGet("/", () => "Felis client is up and running!");

app.MapPost("/dispatch", async (MessageHandler messageHandler, [FromBody] TestModel model, [FromQuery] string topic) =>
{
    await messageHandler.Publish(model, topic);
    return Results.Created("/dispatch", model);
});

app.Run();