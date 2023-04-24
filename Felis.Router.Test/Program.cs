using Felis.Router;

var builder = WebApplication.CreateBuilder(args);

builder.AddFelisRouter();

var app = builder.Build();

app.MapGet("/", () => "Felis router is up and running!");

app.Run();