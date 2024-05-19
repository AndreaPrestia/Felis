namespace Felis.Core.Models;

public record ConsumedMessage(Guid Id, ConnectionId ConnectionId, long Timestamp)
{
    public Guid Id { get; set; } = Id;
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}