using Felis.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Services
{
	internal sealed class ClusterService : IAsyncDisposable
	{
		private readonly HubConnection? _hubConnection;
		private readonly ILogger<ClusterService> _logger;
		private readonly RouterService _routerService;

		public ClusterService(HubConnection? hubConnection, ILogger<ClusterService> logger, RouterService routerService)
		{
			_hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_routerService = routerService ?? throw new ArgumentNullException(nameof(routerService));
		}

		public async Task SubscribeAsync(CancellationToken cancellationToken = default)
		{
			if (_hubConnection == null)
			{
				throw new ArgumentNullException($"Connection to Felis cluster not correctly initialized");
			}

			_hubConnection.On<ErrorMessage?, ConnectionId?>("ErrorMessageMirroring", async (messageIncoming, connectedConsumer) =>
			{
				try
				{
					if (messageIncoming == null)
					{
						_logger.LogWarning("No messageIncoming.");
						return;
					}

					if (messageIncoming.Message == null)
					{
						_logger.LogWarning("No messageIncoming.Message");
						return;
					}

					if (messageIncoming.Message.Header == null)
					{

						_logger.LogWarning("No Header provided.");
						return;
					}

					if (messageIncoming.Message.Header?.Topic == null ||
						string.IsNullOrWhiteSpace(messageIncoming.Message.Header?.Topic?.Value))
					{

						_logger.LogWarning("No Topic provided in Header.");
						return;
					}

					if (string.IsNullOrWhiteSpace(_hubConnection?.ConnectionId))
					{
						_logger.LogWarning("No connection id found. No message will be processed.");
						return;
					}

					var errorSetResult = await _routerService.Error(messageIncoming.Message.Header.Id, messageIncoming);

					_logger.LogInformation($"ErrorMessageMirroring set result {errorSetResult} for connection id {_hubConnection.ConnectionId} from consumer {connectedConsumer?.Value}");
				}
				catch (Exception? ex)
				{
					_logger.LogError(ex, ex.Message);
				}
			});

			_hubConnection.On<ConsumedMessage?, ConnectionId?>("ConsumedMessageMirroring", async (messageIncoming, connectedConsumer) =>
			{
				try
				{
					if (messageIncoming == null)
					{
						_logger.LogWarning("No messageIncoming.");
						return;
					}

					if (messageIncoming.Message == null)
					{
						_logger.LogWarning("No messageIncoming.Message");
						return;
					}

					if (messageIncoming.Message.Header == null)
					{

						_logger.LogWarning("No Header provided.");
						return;
					}

					if (messageIncoming.Message.Header?.Topic == null ||
						string.IsNullOrWhiteSpace(messageIncoming.Message.Header?.Topic?.Value))
					{

						_logger.LogWarning("No Topic provided in Header.");
						return;
					}

					if (string.IsNullOrWhiteSpace(_hubConnection?.ConnectionId))
					{
						_logger.LogWarning("No connection id found. No message will be processed.");
						return;
					}

					var consumeSetResult = await _routerService.Consume(messageIncoming.Message.Header.Id, messageIncoming);

					_logger.LogInformation($"ConsumedMessageMirroring set result {consumeSetResult} for connection id {_hubConnection.ConnectionId} from consumer {connectedConsumer?.Value}");
				}
				catch (Exception? ex)
				{
					_logger.LogError(ex, ex.Message);
				}
			});

			_hubConnection.On<Topic?, ConnectionId?>("PurgeMirroring", async (messageIncoming, connectedConsumer) =>
			{
				try
				{
					if (messageIncoming == null)
					{
						_logger.LogWarning("No message incoming.");
						return;
					}

					if (string.IsNullOrWhiteSpace(messageIncoming.Value))
					{
						_logger.LogWarning("No message incoming value.");
						return;
					}

					if (string.IsNullOrWhiteSpace(_hubConnection?.ConnectionId))
					{
						_logger.LogWarning("No connection id found. No message will be processed.");
						return;
					}

					var purgeResult = await _routerService.PurgeReady(messageIncoming);

					_logger.LogInformation($"PurgeMirroring set result {purgeResult} for connection id {_hubConnection.ConnectionId} from consumer {connectedConsumer?.Value}");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, ex.Message);
				}
			});

			await CheckHubConnectionStateAndStartIt(cancellationToken);
		}

		public async ValueTask DisposeAsync()
		{
			if (_hubConnection != null)
			{
				await _hubConnection?.InvokeAsync("RemoveConnectionId", new ConnectionId(_hubConnection?.ConnectionId))!;
				await _hubConnection!.DisposeAsync();
			}
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

				await _hubConnection?.InvokeAsync("SetConnectionId", cancellationToken)!;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.Message);
			}
		}
	}
}
