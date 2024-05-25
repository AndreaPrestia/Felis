using Felis.Core.Models;
using Felis.Router.Entities;

namespace Felis.Router.Services;

internal sealed class ConnectionService
{
    private static readonly List<ConsumerConnectionEntity> ConnectionMap = new();
    private static readonly string ConsumerConnectionMapLocker = string.Empty;

    public List<Consumer> GetConnectedConsumers(string topic)
    {
        List<Consumer> consumers;

        lock (ConsumerConnectionMapLocker)
        {
            consumers = ConnectionMap.Select(x => x.Consumer)
                .Where(x => x.Topics.Select(t => t).ToList().Contains(topic)).ToList();
        }

        return consumers;
    }

    public List<ConsumerConnectionEntity> GetConnectionIds(string topic)
    {
        List<ConsumerConnectionEntity> consumerConnections;

        lock (ConsumerConnectionMapLocker)
        {
            consumerConnections = ConnectionMap
                .Where(x => x.Consumer.Topics.Select(t => t).ToList().Contains(topic)).ToList();
        }

        return consumerConnections;
    }

    public void KeepConsumerConnection(Consumer consumer, string connectionId)
    {
        lock (ConsumerConnectionMapLocker)
        {
            if (ConnectionMap.All(x =>
                    !x.ConnectionId.Equals(connectionId, StringComparison.InvariantCultureIgnoreCase)))
            {
                ConnectionMap.Add(new ConsumerConnectionEntity()
                {
                    Consumer = consumer,
                    ConnectionId = connectionId,
                });
            }
        }
    }

    public void RemoveConsumerConnections(string connectionId)
    {
        lock (ConsumerConnectionMapLocker)
        {
            var consumers = ConnectionMap.Where(x =>
                !x.ConnectionId.Equals(connectionId, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (consumers.Count == 0) return;

            consumers.ForEach(consumer => ConnectionMap.Remove(consumer));
        }
    }
}
