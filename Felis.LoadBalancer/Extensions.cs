using Felis.LoadBalancer.Configurations;
using Felis.LoadBalancer.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Felis.LoadBalancer;

public static class Extensions
{
       public static void AddFelisLoadBalancer(this IHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
            services.Configure<FelisLoadBalancerConfiguration>(context.Configuration.GetSection(
                FelisLoadBalancerConfiguration.FelisLoadBalancer));

            services.Configure<JsonOptions>(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

            services.AddSignalR();

            services.AddServices();

            services.AddSwagger();
        });
    }

    private static void AddServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<FelisLoadBalancerService>();
    }

    private static void AddSwagger(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddEndpointsApiExplorer();

        serviceCollection.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Felis load balancer",
                Description = "Felis load balancer endpoints",
                Contact = new OpenApiContact
                {
                    Name = "Andrea Prestia",
                    Email = "andrea@prestia.dev",
                    Url = new Uri("https://www.linkedin.com/in/andrea-prestia-5212a2166/"),
                }
            });
        });
    }

    public static void UseFelisRouter(this WebApplication app)
    {
        app.UseSwagger();

        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json",
            $"Felis Load Balancer v1"));
    }
}