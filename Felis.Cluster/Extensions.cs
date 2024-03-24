using Felis.Cluster.Configurations;
using Felis.Cluster.Endpoints;
using Felis.Cluster.Middlewares;
using Felis.Cluster.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Felis.Cluster;

public static class Extensions
{
	public static void AddFelisCluster(this IHostBuilder builder)
	{
		builder.ConfigureServices((context, services) =>
		{
			services.Configure<LoadBalancerConfiguration>(context.Configuration.GetSection(
				LoadBalancerConfiguration.FelisLoadBalancer));

			services.Configure<JsonOptions>(options =>
			{
				options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
			});

			services.AddSwagger();

			services.AddHttpClient<LoadBalancingMiddleware>("loadBalancingClient", (_, _) => { })
				.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
					PooledConnectionLifetime = TimeSpan.FromMinutes(15)
				})
				.SetHandlerLifetime(Timeout.InfiniteTimeSpan);

			services.AddServices();
		});
	}

	private static void AddSwagger(this IServiceCollection serviceCollection)
	{
		serviceCollection.AddEndpointsApiExplorer();

		serviceCollection.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", new OpenApiInfo
			{
				Version = "v1",
				Title = "Felis router",
				Description = "Felis Cluster endpoints",
				Contact = new OpenApiContact
				{
					Name = "Andrea Prestia",
					Email = "andrea@prestia.dev",
					Url = new Uri("https://www.linkedin.com/in/andrea-prestia-5212a2166/"),
				}
			});
		});
	}
	
	private static void AddServices(this IServiceCollection serviceCollection)
	{
		serviceCollection.AddSingleton<LoadBalancingService>();
	}

	public static void UseFelisCluster(this WebApplication app)
	{
		app.UseSwagger();

		app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json",
			"Felis Cluster v1"));
		
        app.MapFelisClusterEndpoints();

		app.UseWhen(
			context => context.Request.Path.ToString().Contains("felis/router") || (context.Request.Path.ToString().EndsWith("/dispatch") && context.Request.Method.Equals("POST")) || ((context.Request.Path.ToString().StartsWith("/messages") ||
					   context.Request.Path.ToString().StartsWith("/consumers")) && context.Request.Method.Equals("GET")),
			appBranch => { appBranch.UseMiddleware<LoadBalancingMiddleware>(); });
	}
}