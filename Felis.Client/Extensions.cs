using Felis.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Felis.Client;

public static class Extensions
{
	public static void UseFelisClient(this WebApplication? app)
	{
		app?.UseResponseCompression();
	}

	public static void AddFelisClient(this WebApplicationBuilder builder)
	{
		var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

		var configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile(string.IsNullOrEmpty(aspNetCoreEnvironment)
				? "appsettings.json"
				: $"appsettings.{aspNetCoreEnvironment}.json");
		var config = configurationBuilder.Build();

		var configuration = config.GetSection("FelisClient").Get<FelisConfiguration>();

		if (string.IsNullOrWhiteSpace(configuration?.RouterEndpoint))
		{
			throw new ArgumentNullException(nameof(configuration.RouterEndpoint));
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
			.WithUrl($"{configuration.RouterEndpoint}/felis/router",
				options => { options.Transports = HttpTransportType.WebSockets; })
			.WithAutomaticReconnect()
			.Build());

		builder.AddConsumers();
	}

	private static void AddConsumers(this WebApplicationBuilder builder)
	{
		builder.Services.AddSingleton<MessageHandler>();

		// Build the intermediate service provider
		var sp = builder.Services.BuildServiceProvider();

		// This will succeed.
		var messageHandler = sp.GetService<MessageHandler>();

		messageHandler?.Subscribe().Wait();

		//TODO autoregister consumers!
		AppDomain.CurrentDomain.GetAssemblies().First(x => x.GetName().Name == AppDomain.CurrentDomain.FriendlyName)
		.GetTypes().Where(t =>
			  t.IsSubclassOf(typeof(Consume<ConsumeEntity>)) && !t.IsInterface
												&& !t.IsAbstract).ToList().ForEach(t =>
												{
													var firstConstructor = t.GetConstructors().FirstOrDefault();

													var parameters = new List<object>();

													if (firstConstructor == null)
													{
														throw new NotImplementedException($"Constructor not implemented in {t.Name}");
													}

													foreach (var param in firstConstructor.GetParameters())
													{
														using var serviceScope = sp.CreateScope();
														var provider = serviceScope.ServiceProvider;

														var service = provider.GetService(param.ParameterType);

														parameters.Add(service!);
													}

													var instance = (Consume<ConsumeEntity>)Activator.CreateInstance(t, parameters.ToArray())!;

													if (instance == null!)
													{
														throw new ApplicationException($"Cannot create an instance of {t.Name}");
													}

													builder.Services.AddSingleton(t, instance);
												});
	}
}