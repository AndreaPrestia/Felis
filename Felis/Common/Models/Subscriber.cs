namespace Felis.Common.Models;

public record Subscriber(string? Hostname, string? IpAddress, List<TopicValue> Topics, List<QueueValue> Queues)
{
    public List<TopicValue> Topics { get; set; } = Topics;
    public List<QueueValue> Queues { get; set; } = Queues;

}

public record TopicValue(string Name, bool Unique, RetryPolicy? RetryPolicy);

public record QueueValue(string Name, bool Unique, RetryPolicy? RetryPolicy);


