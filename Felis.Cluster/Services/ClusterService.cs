using Felis.Cluster.Hubs;
using Felis.Cluster.Managers;
using Felis.Core.Models;
using Felis.LoadBalancer.Services;
using Microsoft.AspNetCore.SignalR;

namespace Felis.Cluster.Services
{

	internal class ClusterService
	{
		private readonly IHubContext<ClusterHub> _hubContext;
		private readonly ConnectionManager _connectionManager;
		private readonly LoadBalancingService _felisRouterLoadBalancingService;

		public ClusterService(IHubContext<ClusterHub> hubContext, ConnectionManager connectionManager, LoadBalancingService felisRouterLoadBalancingService)
		{
			_hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
			_connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
			_felisRouterLoadBalancingService = felisRouterLoadBalancingService ?? throw new ArgumentNullException(nameof(felisRouterLoadBalancingService));
		}

		public List<ConnectionId> GetActiveConnectionIds()
		{
			return _connectionManager.GetConnectedServers().SelectMany(x => x.Value).ToList();
		}
	}
}
