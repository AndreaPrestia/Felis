using Felis.Client;
using Felis.Client.Test.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddFelisClient();

var app = builder.Build();

app.MapGet("/", () => "Felis client is up and running!");

app.MapPost("/dispatch/{topic}", async (MessageHandler messageHandler, [FromBody] TestModel model, [FromRoute] string topic) =>
{
    await messageHandler.PublishAsync(model, topic);
    return Results.Created("/dispatch", model);
});

app.Run();