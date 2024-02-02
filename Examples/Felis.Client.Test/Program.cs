using Felis.Client;
using Felis.Client.Test.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddFelisClient("https://localhost:7103");

var app = builder.Build();

app.MapGet("/", () => "Felis client is up and running!");

app.MapPost("/dispatch/{topic}", async (MessageHandler messageHandler, [FromBody] TestModel model, [FromRoute] string topic) =>
{
    await messageHandler.PublishAsync(model, topic).ConfigureAwait(false);
    return Results.Created("/dispatch", model);
});

app.Run();