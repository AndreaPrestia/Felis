namespace Felis.Core.Models;

public record ConsumedMessage(Guid Id, string ConnectionId, long Timestamp);