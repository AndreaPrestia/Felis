using Felis.Core.Models;

namespace Felis.Router.Managers
{
	internal sealed class FelisConnectionManager 
	{
		private static readonly Dictionary<Consumer, List<ConnectionId>> FelisConnectionMap = new();
		private static readonly string ConsumerConnectionMapLocker = string.Empty;

		public List<Consumer> GetConnectedConsumers(Topic topic)
		{
			List<Consumer> consumers;

			lock (ConsumerConnectionMapLocker)
			{
				consumers = FelisConnectionMap.Select(x => x.Key).Where(x => x.Topics.Select(t => t.Value).ToList().Contains(topic.Value)).ToList();
			}

			return consumers;
		}

		public List<ConnectionId> GetConnectionIds(Topic topic)
		{
			List<ConnectionId> connectionIds;

			lock (ConsumerConnectionMapLocker)
			{
				connectionIds = FelisConnectionMap.Where(x => x.Key.Topics.Select(t => t.Value).ToList().Contains(topic.Value)).SelectMany(e => e.Value).ToList();
			}

			return connectionIds;
		}

		public void KeepConsumerConnection(Consumer consumer, ConnectionId connectionId)
		{
			lock (ConsumerConnectionMapLocker)
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
			lock (ConsumerConnectionMapLocker)
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
