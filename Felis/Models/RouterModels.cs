namespace Felis.Models;

internal record SubscriberModel(string? Hostname, string? IpAddress, List<string> Topics);
internal record MessageModel(Guid Id, string Topic, string? Payload);