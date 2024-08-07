using System.Threading.Channels;
using Felis.Models;

namespace Felis.Entities;

internal class SubscriberEntity
{
    public Guid Id { get; }
    public Channel<MessageModel> MessageChannel { get; }
    public string Hostname { get; }
    public string IpAddress { get; }
    public List<string> Topics { get; }

    public SubscriberEntity(string ipAddress, string hostname, List<string> topics)
    {
        Id = Guid.NewGuid();
        MessageChannel = Channel.CreateUnbounded<MessageModel>();
        Hostname = hostname;
        IpAddress = ipAddress;
        Topics = topics;
    }
}
