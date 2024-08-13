namespace Felis.Models;

internal record SubscriberModel(string? Hostname, string? IpAddress, string Topic);
internal record MessageModel(Guid Id, string Topic, string? Payload);