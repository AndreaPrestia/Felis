using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Felis.Core;
using Felis.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Felis.Client;

public sealed class MessageHandler : IAsyncDisposable
{
	private readonly HubConnection? _hubConnection;
	private readonly ILogger<MessageHandler> _logger;
	private readonly FelisConfiguration _configuration;
	private ConcurrentBag<Message> _messages = new ConcurrentBag<Message>();
	private readonly Guid _handlerId;
	public MessageHandler(HubConnection? hubConnection, ILogger<MessageHandler> logger,
		FelisConfiguration configuration)
	{
		_hubConnection = hubConnection;
		_logger = logger;
		_configuration = configuration;
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

			using var client = new HttpClient();
			var responseMessage = await client.PostAsJsonAsync($"{_configuration.RouterEndpoint}/dispatch",
				new Message(new Topic() { Value = topic ?? payload.GetType().FullName }, payload), cancellationToken: cancellationToken);

			responseMessage.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, ex.Message);
		}
	}

	public async Task Subscribe(string topic, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(topic))
		{
			throw new ArgumentNullException(nameof(topic));
		}

		if (_hubConnection == null)
		{
			throw new ArgumentNullException($"Connection to Felis router not correctly initialized");
		}

		//TODO add check to listen in while for message received

		_hubConnection.On<Message>(topic, async (messageIncoming) =>
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

			if (_messages.Any(m => m.Topic?.Value == topic && m.Timestamp == messageIncoming.Timestamp))
			{
				_messages = new ConcurrentBag<Message>(_messages.Where(m => m.Topic?.Value != topic));
			}

			_messages.Add(messageIncoming);

			using var client = new HttpClient();
			var responseMessage = await client.PostAsJsonAsync($"{_configuration.RouterEndpoint}/consume",
				new ConsumedMessage(messageIncoming, new Core.Models.Client() { Value = _hubConnection.ConnectionId }),
				cancellationToken: cancellationToken);

			responseMessage.EnsureSuccessStatusCode();

			//TODO invoke automatically the consume passing the message
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, ex.Message);
		}
	});

		await CheckHubConnectionStateAndStartIt(cancellationToken);
	}

	public T? Consume<T>(string topic, CancellationToken cancellationToken = default) where T : class
	{
		if (string.IsNullOrWhiteSpace(topic))
		{
			throw new ArgumentNullException(nameof(topic));
		}

		var message = _messages.FirstOrDefault(m => m.Topic?.Value == topic);

		if (message == null || string.IsNullOrWhiteSpace(message.Content))
		{
			return default;
		}

		return JsonSerializer.Deserialize<T>(message.Content);
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
}