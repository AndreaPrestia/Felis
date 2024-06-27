namespace Felis.Router.Entities;

internal sealed class QueueEntity
{
    public Guid Id { get; set; }
    public long Timestamp { get; set; }
    public string? ConnectionId { get; set; }
}

