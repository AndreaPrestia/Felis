using Felis.Router;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFelisRouter();

var app = builder.Build();

app.UseFelisRouter();

app.Run();