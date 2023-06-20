using Felis.Router.Interfaces;

namespace Felis.Router.Managers
{
	public class FelisConnectionManager : IFelisConnectionManager
	{
		private static readonly Dictionary<Guid, List<string>> FelisConnectionMap = new();
		private static readonly string UserConnectionMapLocker = string.Empty;

		public List<string> GetUserConnections(Guid id)
		{
			var connections = new List<string>();

			lock (FelisConnectionMap)
			{
				if (FelisConnectionMap.ContainsKey(id))
				{
					connections = FelisConnectionMap[id];
				}
			}

			return connections;
		}

		public void KeepUserConnection(Guid id, string connectionId)
		{
			lock (UserConnectionMapLocker)
			{
				if (!FelisConnectionMap.ContainsKey(id))
				{
					FelisConnectionMap[id] = new List<string>();
				}
				FelisConnectionMap[id].Add(connectionId);
			}
		}

		public void RemoveUserConnection(string connectionId)
		{
			lock (UserConnectionMapLocker)
			{
				foreach (var userId in FelisConnectionMap.Keys)
				{
					if (FelisConnectionMap.ContainsKey(userId))
					{
						if (FelisConnectionMap[userId].Contains(connectionId))
						{
							FelisConnectionMap[userId].Remove(connectionId);
							break;
						}
					}
				}
			}
		}
	}
}
