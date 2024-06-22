using Felis.Router.Enums;

namespace Felis.Router.Entities;

internal sealed class MessageEntity
{
    public Guid Id { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public long Timestamp { get; set; }
    public long UpdatedAt { get; set; }
    public long EnqueuedAt { get; set; }
    public MessageStatus Status { get; set; }
    public List<MessageError> Errors { get; } = new();
    public List<MessageAcknowledgement> Ack { get; set; } = new();
    public List<MessageRetry> Retries { get; set; } = new();
    public List<MessageProcess> Processes { get; set; } = new();
}

internal class MessageAcknowledgement
{
    public Guid MessageId { get; set; }
    public string ConnectionId { get; set; } = string.Empty;
    public long Timestamp { get; set; }
}

internal class MessageError
{
    public string ConnectionId { get; set; } = string.Empty;
    public List<MessageErrorDetail> Details { get; set; } = new();
    public MessageRetryPolicy? RetryPolicy { get; set; }
}

internal class MessageErrorDetail
{
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public long Timestamp { get; set; }
}

internal class MessageRetryPolicy
{
    public int Attempts { get; set; }
}

internal class MessageRetry
{
    public string ConnectionId { get; set; } = string.Empty;
    public long Timestamp { get; set; }
    public long? Sent { get; set; }
}

internal class MessageProcess
{
    public string ConnectionId { get; set; } = string.Empty;
    public long Timestamp { get; set; } 
    public long ExecutionTimeMs { get; set; }
}