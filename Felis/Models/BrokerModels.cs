namespace Felis.Models;

internal record SubscriberModel(string? Hostname, string? IpAddress, List<string> Topics);
internal record MessageRequestModel(string Topic, string? Payload);
internal record MessageModel(Guid Id, string Topic, string? Payload);