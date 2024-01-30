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
        builder.ConfigureServices((context, serviceCollection) =>
        {
            serviceCollection.Configure<FelisConfiguration>(context.Configuration.GetSection(
                FelisConfiguration.FelisClient));

			var configuration = context.Configuration.GetSection(FelisConfiguration.FelisClient).Get<FelisConfiguration>();

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

			serviceCollection.RegisterConsumers();

			serviceCollection.AddSingleton<ConsumerResolver>();
			serviceCollection.AddSingleton<MessageHandler>();

			serviceCollection.AddHttpClient<MessageHandler>("felisClient", (_, client) =>
				{
					client.BaseAddress = new Uri(configuration.Router?.Endpoint!);
				}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
				{
					PooledConnectionLifetime = TimeSpan.FromMinutes(configuration.Router!.PooledConnectionLifetimeMinutes)
				})
				.SetHandlerLifetime(Timeout.InfiniteTimeSpan);
			
			var serviceProvider = serviceCollection.BuildServiceProvider();

			var messageHandler = serviceProvider.GetService<MessageHandler>();

			messageHandler?.SubscribeAsync().Wait();
		});
	}
	
	private static void RegisterConsumers(this IServiceCollection serviceCollection)
	{
		var consumersTypes = GetConsumersTypesFromCurrentInstance();

		foreach (var consumerType in consumersTypes)
		{
			serviceCollection.AddSingleton(typeof(Consume<>), consumerType);
		}
	}

	private static List<Type> GetConsumersTypesFromCurrentInstance()
	{
		var types = AppDomain.CurrentDomain.GetAssemblies()
			.Where(x => x.GetName().Name == AppDomain.CurrentDomain.FriendlyName).Select(t => t.GetType()).Where(t =>
				t.BaseType?.FullName != null
				&& t.BaseType.FullName.Contains("Felis.Client.Consume") &&
				t is { IsInterface: false, IsAbstract: false }
				&& t.GetCustomAttributes(typeof(TopicAttribute), false).Any()
				&& t.GetMethods().Any(x => x.Name == "Process"
				                           && x.GetParameters().Count() ==
				                           1)).ToList();

		return types;
	}
}