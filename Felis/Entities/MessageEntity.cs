using Felis.Models;

namespace Felis.Entities;

internal class MessageEntity
{
    public Guid Id { get; set; }
    public long? Sent { get; set; }
    public long Timestamp { get; set; }
    public MessageModel Message { get; set; } = null!;
}

