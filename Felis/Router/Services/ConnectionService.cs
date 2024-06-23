using Felis.Common.Models;
using Felis.Router.Entities;

namespace Felis.Router.Services;

internal sealed class ConnectionService
{
    private static readonly List<SubscriberConnectionEntity> ConnectionMap = new();
    private static readonly string SubscriberConnectionMapLocker = string.Empty;
    
    public delegate void NotifyNewConnectedSubscriberEventHandler(object sender, NewSubscriberConnectedEventArgs e);
    public event NotifyNewConnectedSubscriberEventHandler? NotifyNewConnectedSubscriber;

    public List<Common.Models.Subscriber> GetConnectedSubscribers(string topic)
    {
        List<Common.Models.Subscriber> consumers;

        lock (SubscriberConnectionMapLocker)
        {
            consumers = ConnectionMap.Select(x => x.Subscriber)
                .Where(x => x.Topics.Select(t => t).ToList().Contains(topic)).ToList();
        }

        return consumers;
    }

    public List<SubscriberConnectionEntity> GetConnectionIds(string topic)
    {
        List<SubscriberConnectionEntity> consumerConnections;

        lock (SubscriberConnectionMapLocker)
        {
            consumerConnections = ConnectionMap
                .Where(x => x.Subscriber.Topics.Select(t => t).ToList().Contains(topic)).ToList();
        }

        return consumerConnections;
    }
    
    public SubscriberConnectionEntity? GetSubscriberByConnectionId(string connectionId)
    {
        lock (SubscriberConnectionMapLocker)
        {
            return ConnectionMap.FirstOrDefault(x => x.ConnectionId == connectionId);
        }
    }

    public void KeepSubscriberConnection(Common.Models.Subscriber subscriber, string connectionId)
    {
        lock (SubscriberConnectionMapLocker)
        {
            if (ConnectionMap.All(x =>
                    !x.ConnectionId.Equals(connectionId, StringComparison.InvariantCultureIgnoreCase)))
            {
                ConnectionMap.Add(new SubscriberConnectionEntity()
                {
                    Subscriber = subscriber,
                    ConnectionId = connectionId
                });
                
                OnNotifyNewConnectedSubscriber(new NewSubscriberConnectedEventArgs()
                {
                    Subscriber = subscriber,
                    ConnectionId = connectionId
                });
            }
        }
    }

    public void RemoveSubscriberConnections(string connectionId)
    {
        lock (SubscriberConnectionMapLocker)
        {
            var subscribers = ConnectionMap.Where(x =>
                !x.ConnectionId.Equals(connectionId, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (subscribers.Count == 0) return;

            subscribers.ForEach(consumer => ConnectionMap.Remove(consumer));
        }
    }
    
    private void OnNotifyNewConnectedSubscriber(NewSubscriberConnectedEventArgs e)
    {
        NotifyNewConnectedSubscriber?.Invoke(this, e);
    }
}