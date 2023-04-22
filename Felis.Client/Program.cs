using Felis.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddFelisClient();

var app = builder.Build();

app.MapGet("/", () => "Felis client is up and running!");

app.Run();
