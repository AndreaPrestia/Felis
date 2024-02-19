using Felis.Cluster.Managers;
using Felis.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Felis.Cluster.Hubs
{

	[Route("/felis/cluster")]
	internal sealed class ClusterHub : Hub
	{
		private readonly ILogger<ClusterHub> _logger;
		private readonly ConnectionManager _connectionManager;

		public ClusterHub(ILogger<ClusterHub> logger, ConnectionManager connectionManager)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
		}

		public string SetConnectionId()
		{
			try
			{
				var clientIp = Context.GetHttpContext()?.Connection.RemoteIpAddress;

				if (clientIp == null)
				{
					throw new InvalidOperationException($"No Ip address retrieve from Context {Context.ConnectionId}");
				}

				var clientHostname = Dns.GetHostEntry(clientIp).HostName;

				_connectionManager.KeepServerConnection(clientIp.ToString(),
					new ConnectionId(Context.ConnectionId));
				return Context.ConnectionId;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.Message);
				return string.Empty;
			}
		}

		public void RemoveConnectionId(ConnectionId connectionId)
		{
			try
			{
				_connectionManager.RemoveServerConnection(connectionId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.Message);
			}
		}
	}
}
