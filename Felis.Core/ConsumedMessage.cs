namespace Felis.Core;

public record ConsumedMessage(Message? Message, ConnectionId ConnectionId)
{
    public Guid Id { get; } = Guid.NewGuid();
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}