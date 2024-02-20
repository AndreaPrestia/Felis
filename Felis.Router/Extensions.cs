using Felis.Core.Models;
using Felis.Router.Configurations;
using Felis.Router.Endpoints;
using Felis.Router.Hubs;
using Felis.Router.Managers;
using Felis.Router.Services;
using Felis.Router.Services.Background;
using Felis.Router.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace Felis.Router;

public static class Extensions
{
	public static void AddFelisRouter(this IHostBuilder builder)
	{
		builder.ConfigureServices((context, services) =>
		{
			services.Configure<RouterConfiguration>(context.Configuration.GetSection(
				RouterConfiguration.FelisRouter));

			var configuration =
				context.Configuration.GetSection(RouterConfiguration.FelisRouter)
					.Get<RouterConfiguration>() ??
				throw new ApplicationException($"{RouterConfiguration.FelisRouter} configuration not provided");

			if (configuration.MessageConfiguration == null)
			{
				throw new ApplicationException("FelisRouter:MessageConfiguration not provided");
			}

			services.Configure<JsonOptions>(options =>
			{
				options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
			});

			services.AddSignalR();

			services.AddServices();

			services.AddSwagger();

			if(configuration.ClusterConfiguration != null && !string.IsNullOrWhiteSpace(configuration.ClusterConfiguration.Endpoint))
			{
				services.AddSingleton<ClusterService>();

				services.AddResponseCompression(opts =>
				{
					opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
						new[] { "application/octet-stream" });
				});

				var hubConnectionBuilder = new HubConnectionBuilder();

				hubConnectionBuilder.Services.AddSingleton<IConnectionFactory>(
					new HttpConnectionFactory(Options.Create(new HttpConnectionOptions()), NullLoggerFactory.Instance));

				services.AddSingleton(hubConnectionBuilder
					.WithUrl($"{configuration.ClusterConfiguration.Endpoint}/felis/cluster",
						options => { options.Transports = HttpTransportType.WebSockets; })
					.WithAutomaticReconnect()
					.Build());

				var serviceProvider = services.BuildServiceProvider();

				var clusterService = serviceProvider.GetService<ClusterService>();

				clusterService?.SubscribeAsync().Wait();
			}
		});
	}

	private static void AddServices(this IServiceCollection serviceCollection)
	{
		serviceCollection.AddHostedService<RequeueService>();
		serviceCollection.AddHostedService<CleanService>();
		serviceCollection.AddHostedService<SenderService>();
		serviceCollection.AddSingleton<ConnectionManager>();
		serviceCollection.AddSingleton<RouterStorage>();
		serviceCollection.AddSingleton<RouterService>();
		serviceCollection.AddSingleton<RouterHub>();
		serviceCollection.AddSingleton<LoadBalancingService>();
		serviceCollection.AddSingleton<HttpInstanceService>();
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
				Description = "Felis router endpoints",
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
		app.MapHub<RouterHub>("/felis/router");

		app.UseSwagger();

		app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json",
			$"Felis Router v1"));

		app.MapFelisRouterEndpoints();
	}
}