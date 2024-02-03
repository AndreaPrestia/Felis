using Felis.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Felis.Client;

public static class Extensions
{
	public static void AddFelisClient(this IHostBuilder builder, string routerEndpoint, int pooledConnectionLifeTimeMinutes = 15, int maxAttempts = 0)
	{
		if (string.IsNullOrWhiteSpace(routerEndpoint))
		{
			throw new ApplicationException(
				"No routerEndpoint provided. The subscription to Felis Router cannot be done");
		}
		
        builder.ConfigureServices((_, serviceCollection) =>
        {
			serviceCollection.AddSignalR();
			
			serviceCollection.AddResponseCompression(opts =>
			{
				opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
					new[] { "application/octet-stream" });
			});

			var hubConnectionBuilder = new HubConnectionBuilder();

			hubConnectionBuilder.Services.AddSingleton<IConnectionFactory>(
				new HttpConnectionFactory(Options.Create(new HttpConnectionOptions()), NullLoggerFactory.Instance));

			serviceCollection.AddSingleton(hubConnectionBuilder
				.WithUrl($"{routerEndpoint}/felis/router",
					options => { options.Transports = HttpTransportType.WebSockets; })
				.WithAutomaticReconnect()
				.Build());

			serviceCollection.RegisterConsumers();

			serviceCollection.AddSingleton<ConsumerResolver>();
			serviceCollection.AddSingleton<MessageHandler>();

			serviceCollection.AddHttpClient<MessageHandler>("felisClient", (_, client) =>
				{
					client.BaseAddress = new Uri(routerEndpoint);
				}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
				{
					PooledConnectionLifetime = TimeSpan.FromMinutes(pooledConnectionLifeTimeMinutes)
				})
				.SetHandlerLifetime(Timeout.InfiniteTimeSpan);
			
			var serviceProvider = serviceCollection.BuildServiceProvider();

			var messageHandler = serviceProvider.GetService<MessageHandler>();

			messageHandler?.SubscribeAsync(maxAttempts > 0 ? new RetryPolicy(maxAttempts) : null).Wait();
		});
	}
	
	private static void RegisterConsumers(this IServiceCollection serviceCollection)
	{
		var genericInterfaceType = typeof(IConsume<>);

		var implementationTypes = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(assembly => assembly.GetTypes())
			.Where(type => type is { IsClass: true, IsAbstract: false } &&
			               type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceType)).ToList();

		foreach (var implementationType in implementationTypes)
		{
			var closedServiceType = genericInterfaceType.MakeGenericType(implementationType.GetInterfaces()
				.Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceType)
				.GetGenericArguments());

			serviceCollection.AddSingleton(closedServiceType, implementationType);
		}
	}
}