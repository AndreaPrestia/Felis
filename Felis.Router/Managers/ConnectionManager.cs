using Felis.Core.Models;

namespace Felis.Router.Managers
{
	internal sealed class ConnectionManager 
	{
		private static readonly Dictionary<Consumer, List<ConnectionId>> ConnectionMap = new();
		private static readonly string ConsumerConnectionMapLocker = string.Empty;

		public List<Consumer> GetConnectedConsumers(string topic)
		{
			List<Consumer> consumers;

			lock (ConsumerConnectionMapLocker)
			{
				consumers = ConnectionMap.Select(x => x.Key).Where(x => x.Topics.Select(t => t).ToList().Contains(topic)).ToList();
			}

			return consumers;
		}

		public List<ConnectionId> GetConnectionIds(string topic)
		{
			List<ConnectionId> connectionIds;

			lock (ConsumerConnectionMapLocker)
			{
				connectionIds = ConnectionMap.Where(x => x.Key.Topics.Select(t => t).ToList().Contains(topic)).SelectMany(e => e.Value).ToList();
			}

			return connectionIds;
		}

		public void KeepConsumerConnection(Consumer consumer, ConnectionId connectionId)
		{
			lock (ConsumerConnectionMapLocker)
			{
				if (!ConnectionMap.ContainsKey(consumer))
				{
                    ConnectionMap[consumer] = new List<ConnectionId>();
				}
                ConnectionMap[consumer].Add(connectionId);
			}
		}

		public void RemoveConsumerConnections(ConnectionId connectionId)
		{
			lock (ConsumerConnectionMapLocker)
			{
			   var consumers = ConnectionMap.Where(x => x.Value.Contains(connectionId)).ToList();

			   if (!consumers.Any()) return;
			   
			   foreach (var consumer in consumers)
			   {
                   ConnectionMap.Remove(consumer.Key);
			   }
			}
		}
	}
}
