using Felis.Core.Models;

namespace Felis.Router.Entities;

internal class ConsumerConnectionEntity
{
    public string ConnectionId { get; set; } = string.Empty;
    public Consumer Consumer { get; set; } = null!;
    public long Timestamp { get; set; }
}