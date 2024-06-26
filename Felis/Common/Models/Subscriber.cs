namespace Felis.Common.Models;

public record Subscriber(string? Hostname, string? IpAddress, List<TopicValue> Topics)
{
    public List<TopicValue> Topics { get; set; } = Topics;
}

public record TopicValue(string Name, bool Unique);

