using Felis.Core.Models;

namespace Felis.Core;

public record ConsumedMessage(Message? Message, Service? Service)
{
    public Guid Id { get; } = Guid.NewGuid();
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}