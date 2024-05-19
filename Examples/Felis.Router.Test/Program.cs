using Felis.Router;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFelisRouter(builder.Configuration);

var app = builder.Build();

app.UseFelisRouter();

app.Run();