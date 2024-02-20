using Felis.Core.Models;

namespace Felis.Cluster.Managers
{
	internal sealed class ConnectionManager
	{
		private static readonly Dictionary<Consumer, List<ConnectionId>> ConsumerConnectionMap = new();
		private static readonly string ConsumerConnectionMapLocker = string.Empty;
		public Dictionary<Consumer, List<ConnectionId>> ConnectedConsumers => ConsumerConnectionMap;

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
