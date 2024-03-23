namespace Felis.Cluster.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.AddFelisCluster();
            
            var app = builder.Build();

            app.MapGet("/", () => "Felis Cluster is up and running :)");

            app.UseFelisCluster();
            
            app.Run();
        }
    }
}
