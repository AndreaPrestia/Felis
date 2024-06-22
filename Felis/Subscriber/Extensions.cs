using System.Text;
using Felis.Common.Models;
using Felis.Subscriber.Resolvers;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Felis.Subscriber;

public static class Extensions
{
	public static void AddFelisClient(this IHostBuilder builder, string connectionString, bool unique = false, int pooledConnectionLifeTimeMinutes = 15, int maxAttempts = 0)
	{
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			throw new ApplicationException(
				"No connectionString provided. The subscription to Felis Router cannot be done");
		}
		
        builder.ConfigureServices((_, serviceCollection) =>
        {
			var hubConnectionBuilder = new HubConnectionBuilder();

			hubConnectionBuilder.Services.AddSingleton<IConnectionFactory>(
				new HttpConnectionFactory(Options.Create(new HttpConnectionOptions()), NullLoggerFactory.Instance));

			var uri = new Uri(connectionString);

			var credentials = Convert.ToBase64String(Encoding.Default.GetBytes(uri.UserInfo));

			var routerEndpoint = $"{uri.Scheme}://{uri.Authority}";
			
			serviceCollection.AddSingleton(hubConnectionBuilder
				.WithUrl($"{connectionString}/felis/router",
					options =>
					{
						options.Transports = HttpTransportType.WebSockets;
						options.Headers.Add("Authorization", $"Basic {credentials}");
					})
				.WithAutomaticReconnect()
				.Build());

			serviceCollection.RegisterConsumers();

			serviceCollection.AddSingleton<ConsumerResolver>();
			serviceCollection.AddSingleton<MessageHandler>();

			serviceCollection.AddHttpClient<MessageHandler>("felisClient", (_, client) =>
				{
					client.BaseAddress = new Uri(routerEndpoint);
					client.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
				}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
				{
					PooledConnectionLifetime = TimeSpan.FromMinutes(pooledConnectionLifeTimeMinutes)
				})
				.SetHandlerLifetime(Timeout.InfiniteTimeSpan);
			
			var serviceProvider = serviceCollection.BuildServiceProvider();

			var messageHandler = serviceProvider.GetService<MessageHandler>();

			messageHandler?.SubscribeAsync(maxAttempts > 0 ? new RetryPolicy(maxAttempts) : null, unique).Wait();
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