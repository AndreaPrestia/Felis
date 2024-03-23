using Felis.Client.Cluster.Test.One.Models;
using Microsoft.AspNetCore.Mvc;

namespace Felis.Client.Cluster.Test.One
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.AddFelisClient("https://localhost:7058", "FriendlyName", 15, 2);

            builder.Services.AddEndpointsApiExplorer();

            var app = builder.Build();

            app.MapGet("/", () => "Felis client cluster one is up and running!").ExcludeFromDescription();

            app.MapPost("/dispatch", async (MessageHandler messageHandler, [FromBody] TestModel model, [FromQuery] string topic) =>
            {
                await messageHandler.PublishAsync(model, topic);
                return Results.Created("/dispatch", model);
            });

            app.Run();
        }
    }
}
