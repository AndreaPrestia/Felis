using Felis.Cluster.Hubs;
using Felis.Cluster.Managers;
using Felis.Core.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Felis.Cluster.Services
{
	internal sealed class MirroringService
	{
		private readonly IHubContext<ClusterHub> _hubContext;
		private readonly ILogger<MirroringService> _logger;
		private readonly ConnectionManager _connectionManager;

		public MirroringService(ILogger<MirroringService> logger, IHubContext<ClusterHub> hubContext, ConnectionManager connectionManager)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
			_connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
		}

		public async Task ErrorAsync(ConnectionId connectionId, ErrorMessage errorMessage, CancellationToken token)
		{
			var connectedConsumers = _connectionManager.ConnectedConsumers.Where(x => !x.Value.Select(c => c.Value).Contains(connectionId.Value)).SelectMany(e => e.Value).ToList();

			if (!connectedConsumers.Any())
			{
				_logger.LogWarning($"No connected consumers other than {connectionId.Value}. No dispatch will be done.");
				return;
			}

			await Parallel.ForEachAsync(connectedConsumers, token, async (connectedConsumer, cancellationToken) =>
            {
                await _hubContext.Clients.Client(connectedConsumer.Value!).SendAsync("ErrorMessageMirroring", errorMessage, connectedConsumer, token);
            }).ConfigureAwait(false);
		}

		public async Task ConsumeAsync(ConnectionId connectionId, ConsumedMessage consumedMessage, CancellationToken token)
		{
			var connectedConsumers = _connectionManager.ConnectedConsumers.Where(x => !x.Value.Select(c => c.Value).Contains(connectionId.Value)).SelectMany(e => e.Value).ToList();

			if (!connectedConsumers.Any())
			{
				_logger.LogWarning($"No connected consumers other than {connectionId.Value}. No consume will be done.");
				return;
			}

			await Parallel.ForEachAsync(connectedConsumers, token, async (connectedConsumer, cancellationToken) =>
            {
                await _hubContext.Clients.Client(connectedConsumer.Value!).SendAsync("ConsumedMessageMirroring", consumedMessage, connectedConsumer, token);
            }).ConfigureAwait(false);
		}

		public async Task PurgeReadyAsync(ConnectionId connectionId, Topic topic, CancellationToken token)
		{
			var connectedConsumers = _connectionManager.ConnectedConsumers.Where(x => !x.Value.Select(c => c.Value).Contains(connectionId.Value)).SelectMany(e => e.Value).ToList();

			if (!connectedConsumers.Any())
			{
				_logger.LogWarning($"No connected consumers other than {connectionId.Value}. No dispatch will be done.");
				return;
			}

			await Parallel.ForEachAsync(connectedConsumers, token, async (connectedConsumer, cancellationToken) =>
			{
				await _hubContext.Clients.Client(connectedConsumer.Value!).SendAsync("PurgeMirroring", topic, connectedConsumer, token);
			});
		}
	}
}
