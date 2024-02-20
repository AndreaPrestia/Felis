using Felis.Cluster.Configurations;
using Felis.Cluster.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Felis.Cluster;

public static class Extensions
{
	public static void AddFelisLoadBalancer(this IHostBuilder builder)
	{
		builder.ConfigureServices((context, services) =>
		{
			services.Configure<LoadBalancerConfiguration>(context.Configuration.GetSection(
				LoadBalancerConfiguration.FelisLoadBalancer));

			services.Configure<JsonOptions>(options =>
			{
				options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
			});

			services.AddHttpClient<LoadBalancingMiddleware>("loadBalancingClient", (_, _) => { })
				.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
				{
					PooledConnectionLifetime = TimeSpan.FromMinutes(15)
				})
				.SetHandlerLifetime(Timeout.InfiniteTimeSpan);

			services.AddServices();
		});
	}

	private static void AddServices(this IServiceCollection serviceCollection)
	{
		serviceCollection.AddSingleton<LoadBalancingMiddleware>();
	}

	public static void UseFelisLoadBalancer(this IApplicationBuilder app)
	{
		app.UseWhen(
			context => context.Request.Path.ToString().EndsWith("/dispatch") || ((context.Request.Path.ToString().StartsWith("/messages") ||
					   context.Request.Path.ToString().StartsWith("/consumers")) && context.Request.Method.Equals("GET")),
			appBranch => { appBranch.UseMiddleware<LoadBalancingMiddleware>(); });
	}
}