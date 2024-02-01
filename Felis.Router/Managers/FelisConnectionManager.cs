using Felis.Core;
using Felis.Core.Models;
using Felis.Router.Interfaces;

namespace Felis.Router.Managers
{
	internal class FelisConnectionManager : IFelisConnectionManager
	{
		private static readonly Dictionary<Service, List<ConnectionId>> FelisConnectionMap = new();
		private static readonly string UserConnectionMapLocker = string.Empty;

		public List<ConnectionId> GetServiceConnections(Service service)
		{
			List<ConnectionId> connections;

			lock (UserConnectionMapLocker)
			{ 
				connections = FelisConnectionMap.Where(x => x.Key == service).SelectMany(x => x.Value).ToList();
			}

			return connections;
		}

		public List<Service> GetConnectedServices()
		{
			List<Service> services;

			lock (UserConnectionMapLocker)
			{
				services = FelisConnectionMap.Select(x => x.Key).ToList();
			}

			return services;
		}

		public void KeepServiceConnection(Service service, ConnectionId connectionId)
		{
			lock (UserConnectionMapLocker)
			{
				if (!FelisConnectionMap.ContainsKey(service))
				{
					FelisConnectionMap[service] = new List<ConnectionId>();
				}
				FelisConnectionMap[service].Add(connectionId);
			}
		}

		public void RemoveServiceConnections(ConnectionId connectionId)
		{
			lock (UserConnectionMapLocker)
			{
			   var services = FelisConnectionMap.Where(x => x.Value.Contains(connectionId)).ToList();

			   if (!services.Any()) return;
			   
			   foreach (var service in services)
			   {
				   FelisConnectionMap.Remove(service.Key);
			   }
			}
		}
	}
}
