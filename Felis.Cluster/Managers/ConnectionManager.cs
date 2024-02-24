using Felis.Core.Models;

namespace Felis.Cluster.Managers
{
    internal sealed class ConnectionManager
    {
        private readonly Dictionary<Consumer, List<ConnectionId>> _consumerConnectionMap = new();
        private readonly string _consumerConnectionMapLocker = string.Empty;
        public Dictionary<Consumer, List<ConnectionId>> ConnectedConsumers
        {
            get
            {
                lock (_consumerConnectionMapLocker)
                {
                    return _consumerConnectionMap;
                }
            }
        }

        public void KeepConsumerConnection(Consumer consumer, ConnectionId connectionId)
        {
            lock (_consumerConnectionMapLocker)
            {
                if (!_consumerConnectionMap.ContainsKey(consumer))
                {
                    _consumerConnectionMap[consumer] = new List<ConnectionId>();
                }
                _consumerConnectionMap[consumer].Add(connectionId);
            }
        }

        public void RemoveConsumerConnections(ConnectionId connectionId)
        {
            lock (_consumerConnectionMapLocker)
            {
                var consumers = _consumerConnectionMap.Where(x => x.Value.Contains(connectionId)).ToList();

                if (!consumers.Any()) return;

                foreach (var consumer in consumers)
                {
                    _consumerConnectionMap.Remove(consumer.Key);
                }
            }
        }
    }
}
