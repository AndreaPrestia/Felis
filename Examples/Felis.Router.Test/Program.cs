using Felis.Router;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddFelisRouter();

var app = builder.Build();

app.UseFelisRouter();

app.MapGet("/", () => "Hello World!");

app.Run();