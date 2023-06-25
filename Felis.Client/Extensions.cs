using Felis.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Felis.Client;

public static class Extensions
{
	public static void AddFelisClient(this IHostBuilder builder)
	{
		builder.ConfigureServices(serviceCollection =>
		{
			var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

			var configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile(string.IsNullOrEmpty(aspNetCoreEnvironment)
					? "appsettings.json"
					: $"appsettings.{aspNetCoreEnvironment}.json");
			var config = configurationBuilder.Build();

			var configuration = config.GetSection("FelisClient").Get<FelisConfiguration>();

			if (string.IsNullOrWhiteSpace(configuration?.Router?.Endpoint))
			{
				throw new ArgumentNullException($"No Router:Endpoint configuration provided");
			}

			serviceCollection.AddSignalR();
			
			serviceCollection.AddResponseCompression(opts =>
			{
				opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
					new[] { "application/octet-stream" });
			});

			serviceCollection.AddSingleton(configuration);

			var hubConnectionBuilder = new HubConnectionBuilder();

			hubConnectionBuilder.Services.AddSingleton<IConnectionFactory>(
				new HttpConnectionFactory(Options.Create(new HttpConnectionOptions()), NullLoggerFactory.Instance));

			serviceCollection.AddSingleton(hubConnectionBuilder
				.WithUrl($"{configuration.Router?.Endpoint}/felis/router",
					options => { options.Transports = HttpTransportType.WebSockets; })
				.WithAutomaticReconnect()
				.Build());

			serviceCollection.AddSingleton<MessageHandler>();

			var serviceProvider = serviceCollection.BuildServiceProvider();

			var messageHandler = serviceProvider.GetService<MessageHandler>();

			messageHandler?.Subscribe().Wait();
		});
	}
	
	public static void AddFelisClientWeb(this WebApplicationBuilder builder)
	{
		var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

		var configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile(string.IsNullOrEmpty(aspNetCoreEnvironment)
				? "appsettings.json"
				: $"appsettings.{aspNetCoreEnvironment}.json");
		var config = configurationBuilder.Build();

		var configuration = config.GetSection("FelisClient").Get<FelisConfiguration>();

		if (string.IsNullOrWhiteSpace(configuration?.Router?.Endpoint))
		{
			throw new ArgumentNullException($"No Router:Endpoint configuration provided");
		}

		builder.Services.AddSignalR();
		builder.Services.AddResponseCompression(opts =>
		{
			opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
				  new[] { "application/octet-stream" });
		});

		builder.Services.AddSingleton(configuration);

		var hubConnectionBuilder = new HubConnectionBuilder();

		hubConnectionBuilder.Services.AddSingleton<IConnectionFactory>(
			new HttpConnectionFactory(Options.Create(new HttpConnectionOptions()), NullLoggerFactory.Instance));

		builder.Services.AddSingleton(hubConnectionBuilder
			.WithUrl($"{configuration.Router?.Endpoint}/felis/router",
				options => { options.Transports = HttpTransportType.WebSockets; })
			.WithAutomaticReconnect()
			.Build());

		builder.Services.AddSingleton<MessageHandler>();

		var serviceProvider = builder.Services.BuildServiceProvider();

		var messageHandler = serviceProvider.GetService<MessageHandler>();

		messageHandler?.Subscribe().Wait();
	}
}