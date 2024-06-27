namespace Felis.Common.Models;

public record MessageRequest(Guid Id, string Topic, string Payload);

public record Message(Header? Header, Content? Content, string Status);

public record Header(Guid Id, string Topic, long Timestamp);

public record Content(string? Payload);

public record ConsumedMessage(Guid Id, string ConnectionId, long Timestamp);

public record ErrorMessage(Guid Id, Message Message, List<ErrorMessageDetail> Errors);

public record ErrorMessageDetail(string ConnectionId, List<ErrorDetail> Details, RetryPolicy? RetryPolicy);
public record ErrorDetail(string? Title, string? Detail);

public record ErrorMessageRequest(Guid Id, string ConnectionId, ErrorDetail Error)
{
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}

public record RetryPolicy(int Attempts);

public record ProcessedMessage(Guid Id, string ConnectionId, long ExecutionTimeMs);

public record NextMessageSentResponse(Guid MessageId, MessageSendStatus MessageSendStatus);

public enum MessageSendStatus
{
    MessageReady,
    NothingToSend,
    MessageNotFound,
    MessageNotReady,
    MessageWithoutTopic,
    NoConnectionIdAvailableForTopic,
    ConnectionIdNotFound,
    MessageSent,
    MessageNotSent
}

public enum MessageStatus
{
    Ready,
    Sent,
    Consumed,
    Processed,
    Error
}


