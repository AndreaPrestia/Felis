using Felis.Router;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFelisRouter("gabriele", "paolini");

var app = builder.Build();

app.UseFelisRouter();

app.Run();