using Felis.Core.Models;

namespace Felis.Cluster.Managers
{
	internal sealed class ConnectionManager
	{
		private static readonly Dictionary<string, List<ConnectionId>> ConnectionMap = new();
		private static readonly string ConnectionMapLocker = string.Empty;

		public Dictionary<string, List<ConnectionId>> GetConnectedServers()
		{
			lock (ConnectionMapLocker)
			{
				return ConnectionMap;
			}
		}

		public void KeepServerConnection(string server, ConnectionId connectionId)
		{
			lock (ConnectionMapLocker)
			{
				if (!ConnectionMap.ContainsKey(server))
				{
					ConnectionMap[server] = new List<ConnectionId>();
				}
				ConnectionMap[server].Add(connectionId);
			}
		}

		public void RemoveServerConnection(ConnectionId connectionId)
		{
			lock (ConnectionMapLocker)
			{
				var servers = ConnectionMap.Where(x => x.Value.Contains(connectionId)).ToList();

				if (!servers.Any()) return;

				foreach (var server in servers)
				{
					ConnectionMap.Remove(server.Key);
				}
			}
		}
	}
}
