using Felis.Core.Models;

namespace Felis.Router.Managers
{
	internal sealed class ConnectionManager
	{
		private static readonly Dictionary<Consumer, List<ConnectionId>> ConsumerConnectionMap = new();
		private static readonly string ConsumerConnectionMapLocker = string.Empty;

		public List<Consumer> GetConnectedConsumers(Topic topic)
		{
			List<Consumer> consumers;

			lock (ConsumerConnectionMapLocker)
			{
				consumers = ConsumerConnectionMap.Select(x => x.Key).Where(x => x.Topics.Select(t => t.Value).ToList().Contains(topic.Value)).ToList();
			}

			return consumers;
		}

		public List<ConnectionId> GetConnectionIds(Topic topic)
		{
			List<ConnectionId> connectionIds;

			lock (ConsumerConnectionMapLocker)
			{
				connectionIds = ConsumerConnectionMap.Where(x => x.Key.Topics.Select(t => t.Value).ToList().Contains(topic.Value)).SelectMany(e => e.Value).ToList();
			}

			return connectionIds;
		}

		public void KeepConsumerConnection(Consumer consumer, ConnectionId connectionId)
		{
			lock (ConsumerConnectionMapLocker)
			{
				if (!ConsumerConnectionMap.ContainsKey(consumer))
				{
					ConsumerConnectionMap[consumer] = new List<ConnectionId>();
				}
				ConsumerConnectionMap[consumer].Add(connectionId);
			}
		}

		public void RemoveConsumerConnections(ConnectionId connectionId)
		{
			lock (ConsumerConnectionMapLocker)
			{
				var consumers = ConsumerConnectionMap.Where(x => x.Value.Contains(connectionId)).ToList();

				if (!consumers.Any()) return;

				foreach (var consumer in consumers)
				{
					ConsumerConnectionMap.Remove(consumer.Key);
				}
			}
		}
	}
}
