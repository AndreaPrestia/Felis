using System.Net;
using Felis.Cluster.Managers;
using Felis.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Felis.Cluster.Hubs
{
	[Route("felis/cluster")]
	internal sealed class ClusterHub : Hub
	{
		private readonly ILogger<ClusterHub> _logger;
		private readonly ConnectionManager _felisConnectionManager;

		public ClusterHub(ILogger<ClusterHub> logger, ConnectionManager felisConnectionManager)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_felisConnectionManager = felisConnectionManager ?? throw new ArgumentNullException(nameof(felisConnectionManager));
		}

		public string SetConnectionId(List<Topic> topics, string friendlyName)
		{
			try
			{
				var clientIp = Context.GetHttpContext()?.Connection.RemoteIpAddress;

				if (clientIp == null)
				{
					throw new InvalidOperationException($"No Ip address retrieve from Context {Context.ConnectionId}");
				}

				var clientHostname = Dns.GetHostEntry(clientIp).HostName;

				_felisConnectionManager.KeepConsumerConnection(new Consumer(friendlyName, clientHostname, clientIp.ToString(), topics),
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
				_felisConnectionManager.RemoveConsumerConnections(connectionId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.Message);
			}
		}
	}
}
