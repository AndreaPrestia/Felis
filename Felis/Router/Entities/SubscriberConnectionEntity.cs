namespace Felis.Router.Entities;

internal class SubscriberConnectionEntity
{
    public string ConnectionId { get; set; } = string.Empty;
    public Common.Models.Subscriber Subscriber { get; set; } = null!;
    public long Timestamp { get; set; }
}