namespace Felis.Router.Entities;

internal sealed class QueueEntity
{
    public Guid MessageId { get; set; }
    public long Timestamp { get; set; }
}

