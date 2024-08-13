using System.Threading.Channels;
using Felis.Models;

namespace Felis.Entities;

internal class SubscriberEntity
{
    public Guid Id { get; }
    public Channel<MessageModel> MessageChannel { get; }
    public string Hostname { get; }
    public string IpAddress { get; }
    public string Topic { get; }

    public SubscriberEntity(string ipAddress, string hostname, string topic)
    {
        Id = Guid.NewGuid();
        MessageChannel = Channel.CreateUnbounded<MessageModel>();
        Hostname = hostname;
        IpAddress = ipAddress;
        Topic = topic;
    }
}
