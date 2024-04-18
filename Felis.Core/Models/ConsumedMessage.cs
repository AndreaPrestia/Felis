namespace Felis.Core.Models;

public record ConsumedMessage(Guid Id, Message? Message, ConnectionId ConnectionId)
{
    public Guid Id { get; set; } = Id;
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}