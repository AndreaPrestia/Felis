namespace Felis.Client.Models;

public record MessageRequest(Guid Id, string Topic, string Payload);

public record Message(Header? Header, Content? Content, string Status);

public record Header(Guid Id, string Topic, long Timestamp);

public record Content(string? Payload);

public record ConsumedMessage(Guid Id, string ConnectionId, long Timestamp);

public record ErrorDetail(string? Title, string? Detail);

public record ErrorMessageRequest(Guid Id, string ConnectionId, ErrorDetail Error, RetryPolicy? RetryPolicy)
{
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}

public record RetryPolicy(int Attempts);