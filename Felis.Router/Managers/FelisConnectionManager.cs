using Felis.Core.Models;
using Felis.Router.Interfaces;

namespace Felis.Router.Managers
{
	public class FelisConnectionManager : IFelisConnectionManager
	{
		private static readonly Dictionary<Service, List<string>> FelisConnectionMap = new();
		private static readonly string UserConnectionMapLocker = string.Empty;

		public List<string> GetServiceConnections(Guid id)
		{
			List<string> connections;

			lock (FelisConnectionMap)
			{ 
				connections = FelisConnectionMap.Where(x => x.Key.Id == id).SelectMany(x => x.Value).ToList();
			}

			return connections;
		}

		public List<Service> GetConnectedServices()
		{
			List<Service> services;

			lock (FelisConnectionMap)
			{
				services = FelisConnectionMap.Where(x => x.Key.IsPublic).Select(x => x.Key).ToList();
			}

			return services;
		}

		public void KeepServiceConnection(Service service, string connectionId)
		{
			lock (UserConnectionMapLocker)
			{
				if (!FelisConnectionMap.ContainsKey(service))
				{
					FelisConnectionMap[service] = new List<string>();
				}
				FelisConnectionMap[service].Add(connectionId);
			}
		}

		public void RemoveServiceConnections(Service service)
		{
			lock (UserConnectionMapLocker)
			{
				 FelisConnectionMap.Remove(service);
			}
		}
	}
}
