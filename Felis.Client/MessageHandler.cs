using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Felis.Core;
using Felis.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Felis.Client;

public sealed class MessageHandler : IAsyncDisposable
{
	private readonly HubConnection? _hubConnection;
	private readonly ILogger<MessageHandler> _logger;
	private readonly FelisConfiguration _configuration;
	private ConcurrentBag<Message> _messages = new ConcurrentBag<Message>();
	private readonly Guid _handlerId;
	private readonly IServiceProvider _serviceProvider;
	private readonly string _topic = "NewDispatchedMethod";

	public MessageHandler(HubConnection? hubConnection, ILogger<MessageHandler> logger,
		FelisConfiguration configuration, IServiceProvider serviceProvider)
	{
		_hubConnection = hubConnection;
		_logger = logger;
		_configuration = configuration;
		_serviceProvider = serviceProvider;
		_handlerId = Guid.NewGuid();
	}

	public async Task Publish<T>(T payload, string? topic, CancellationToken cancellationToken = default)
		where T : class
	{
		if (payload == null)
		{
			throw new ArgumentNullException(nameof(payload));
		}

		try
		{
			await CheckHubConnectionStateAndStartIt(cancellationToken);

			//TODO add an authorization token as parameter

			var type = payload.GetType().FullName;

			using var client = new HttpClient();
			var responseMessage = await client.PostAsJsonAsync($"{_configuration.RouterEndpoint}/dispatch",
				new Message(new Topic() { Value = topic ?? type }, payload, type), cancellationToken: cancellationToken);

			responseMessage.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, ex.Message);
		}
	}

	internal async Task Subscribe(CancellationToken cancellationToken = default)
	{
		if (_hubConnection == null)
		{
			throw new ArgumentNullException($"Connection to Felis router not correctly initialized");
		}

		_hubConnection.On<Message>(_topic, async (messageIncoming) =>
		{
			try
			{
				if (messageIncoming == null)
				{
					throw new ArgumentNullException(nameof(messageIncoming));
				}

				if (string.IsNullOrWhiteSpace(_hubConnection?.ConnectionId))
				{
					throw new ArgumentNullException(nameof(_hubConnection.ConnectionId));
				}

				if (_messages.Any(m => m.Topic?.Value == messageIncoming?.Topic?.Value && m.Timestamp == messageIncoming?.Timestamp))
				{
					_messages = new ConcurrentBag<Message>(_messages.Where(m => m.Topic?.Value != messageIncoming?.Topic?.Value));
				}

				_messages.Add(messageIncoming);

				var type = GetEntityType(messageIncoming.Type);

				if (type == null)
				{
					throw new ArgumentNullException(nameof(type));
				}

				var entity = Deserialize(messageIncoming.Content, type);

				if (entity == null)
				{
					throw new ArgumentNullException(nameof(entity));
				}

				var consumer = FindConsumer(messageIncoming?.Topic?.Value);

				if (consumer == null)
				{
					throw new ArgumentNullException(nameof(consumer));
				}

				using var client = new HttpClient();
				var responseMessage = await client.PostAsJsonAsync($"{_configuration.RouterEndpoint}/consume",
					new ConsumedMessage(messageIncoming, new Core.Models.Client() { Value = _hubConnection.ConnectionId }),
					cancellationToken: cancellationToken);

				responseMessage.EnsureSuccessStatusCode();

				consumer.Process(entity);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.Message);
			}
		});

		await CheckHubConnectionStateAndStartIt(cancellationToken);
	}

	private async Task CheckHubConnectionStateAndStartIt(CancellationToken cancellationToken = default)
	{
		try
		{
			if (_hubConnection == null)
			{
				throw new ArgumentNullException(nameof(_hubConnection));
			}

			if (_hubConnection?.State == HubConnectionState.Disconnected)
			{
				await _hubConnection?.StartAsync(cancellationToken)!;
			}

			await _hubConnection?.InvokeAsync("SetConnectionId", _handlerId, cancellationToken)!;

		}
		catch (Exception ex)
		{
			_logger.LogError(ex, ex.Message);
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (_hubConnection != null)
		{
			await _hubConnection.DisposeAsync();
		}
	}

	#region PrivateMethods
	private Consume<ConsumeEntity>? FindConsumer(string? topic)
	{
		return AppDomain.CurrentDomain.GetAssemblies()
			.First(x => x.GetName().Name == AppDomain.CurrentDomain.FriendlyName).GetTypes().Where(t =>
				t.IsSubclassOf(typeof(Consume<>))
				&& !t.IsAbstract && !t.IsInterface).Where(tp => tp.GetMethods().Any(n =>
				n.GetCustomAttributes<TopicAttribute>().Any(x => x.Value == topic)))
			.Select(t =>
			{
				var firstConstructor = t.GetConstructors().FirstOrDefault();

				var parameters = new List<object>();

				if (firstConstructor == null)
				{
					throw new NotImplementedException($"Not implemented constructor in ${t.Name}");
				}

				foreach (var param in firstConstructor.GetParameters())
				{
					using var serviceScope = _serviceProvider.CreateScope();
					var provider = serviceScope.ServiceProvider;

					var service = provider.GetService(param.ParameterType);

					if (service == null)
					{
						throw new ArgumentNullException("Service not correctly injected");
					}

					parameters.Add(service);
				}

				return (Consume<ConsumeEntity>?)Activator.CreateInstance(t, parameters.ToArray());
			}).FirstOrDefault();
	}

	private static Type? GetEntityType(string? type)
	{
		return AppDomain.CurrentDomain.GetAssemblies()
			.First(x => x.GetName().Name == AppDomain.CurrentDomain.FriendlyName).GetTypes()
			.FirstOrDefault(x => x.IsSubclassOf(typeof(ConsumeEntity)) && string.Equals(type, x.FullName));
	}

	private ConsumeEntity? Deserialize(string? content, Type type)
	{
		return string.IsNullOrWhiteSpace(content) ? null : (ConsumeEntity?)JsonSerializer.Deserialize(content, type);
	}
	#endregion
}