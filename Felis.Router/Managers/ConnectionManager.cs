using Felis.Core.Models;

namespace Felis.Router.Managers
{
	internal sealed class ConnectionManager 
	{
		private static readonly Dictionary<Consumer, List<string>> ConnectionMap = new();
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

		public List<string> GetConnectionIds(string topic)
		{
			List<string> connectionIds;

			lock (ConsumerConnectionMapLocker)
			{
				connectionIds = ConnectionMap.Where(x => x.Key.Topics.Select(t => t).ToList().Contains(topic)).SelectMany(e => e.Value).ToList();
			}

			return connectionIds;
		}

		public void KeepConsumerConnection(Consumer consumer, string connectionId)
		{
			lock (ConsumerConnectionMapLocker)
			{
				if (!ConnectionMap.ContainsKey(consumer))
				{
                    ConnectionMap[consumer] = new List<string>();
				}
                ConnectionMap[consumer].Add(connectionId);
			}
		}

		public void RemoveConsumerConnections(string connectionId)
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
