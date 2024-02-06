using Felis.Core;
using Felis.Core.Models;

namespace Felis.Router.Managers
{
	internal sealed class FelisConnectionManager 
	{
		private static readonly Dictionary<Consumer, List<ConnectionId>> FelisConnectionMap = new();
		private static readonly string UserConnectionMapLocker = string.Empty;

		public List<ConnectionId> GetConsumerConnections(Consumer consumer)
		{
			List<ConnectionId> connections;

			lock (UserConnectionMapLocker)
			{ 
				connections = FelisConnectionMap.Where(x => x.Key == consumer).SelectMany(x => x.Value).ToList();
			}

			return connections;
		}

		public List<Consumer> GetConnectedConsumers()
		{
			List<Consumer> consumers;

			lock (UserConnectionMapLocker)
			{
				consumers = FelisConnectionMap.Select(x => x.Key).ToList();
			}

			return consumers;
		}

		public void KeepConsumerConnection(Consumer consumer, ConnectionId connectionId)
		{
			lock (UserConnectionMapLocker)
			{
				if (!FelisConnectionMap.ContainsKey(consumer))
				{
					FelisConnectionMap[consumer] = new List<ConnectionId>();
				}
				FelisConnectionMap[consumer].Add(connectionId);
			}
		}

		public void RemoveConsumerConnections(ConnectionId connectionId)
		{
			lock (UserConnectionMapLocker)
			{
			   var consumers = FelisConnectionMap.Where(x => x.Value.Contains(connectionId)).ToList();

			   if (!consumers.Any()) return;
			   
			   foreach (var consumer in consumers)
			   {
				   FelisConnectionMap.Remove(consumer.Key);
			   }
			}
		}
	}
}
