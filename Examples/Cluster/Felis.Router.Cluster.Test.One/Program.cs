namespace Felis.Router.Cluster.Test.One
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.AddFelisRouter();

            var app = builder.Build();

            app.UseFelisRouter();

            app.MapGet("/", () => "Felis Router one is up and running!").ExcludeFromDescription();

            app.Run();
        }
    }
}
