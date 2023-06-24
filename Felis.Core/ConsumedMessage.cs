using Felis.Core.Models;

namespace Felis.Core;

public record ConsumedMessage(Message? Message, Service? Service)
{
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}