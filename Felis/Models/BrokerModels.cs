namespace Felis.Models;

internal record SubscriberModel(string? Hostname, string? IpAddress, string Topic, long Timestamp);

internal record MessageModel(Guid Id, string Topic, string? Payload, long Timestamp)
{
    public long? Sent { get; set; }
};