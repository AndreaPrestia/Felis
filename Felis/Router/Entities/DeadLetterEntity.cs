namespace Felis.Router.Entities;

internal sealed class DeadLetterEntity
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public long Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
}