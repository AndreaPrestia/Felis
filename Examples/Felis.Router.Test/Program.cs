using Felis.Router;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddFelisRouter();

var app = builder.Build();

app.UseFelisRouter();

app.MapGet("/", () => "Felis Router is up and running!").ExcludeFromDescription();

app.Run();