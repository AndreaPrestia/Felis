using Felis.Core.Models;

namespace Felis.Router.Managers
{
	internal sealed class FelisConnectionManager 
	{
		private static readonly Dictionary<Consumer, List<ConnectionId>> FelisConnectionMap = new();
		private static readonly string ConsumerConnectionMapLocker = string.Empty;

		public List<Consumer> GetConnectedConsumers()
		{
			List<Consumer> consumers;

			lock (ConsumerConnectionMapLocker)
			{
				consumers = FelisConnectionMap.Select(x => x.Key).ToList();
			}

			return consumers;
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
