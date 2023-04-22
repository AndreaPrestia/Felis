namespace Felis.Core;

public record Message
{
    public string Topic { get; }
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    public object Content { get; }

    private Message(string? topic, object content)
    {
        Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public static Message From(string? topic, object content)
    {
        return new Message(topic, content);
    }
}