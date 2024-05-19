namespace Felis.Core.Models
{
	public record ErrorMessage(Guid Id, Message Message, List<ErrorMessageDetail> Errors)
	{
		public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
	}

    public record ErrorMessageDetail(ConnectionId? ConnectionId, List<ErrorDetail> Details, RetryPolicy? RetryPolicy);
	public record ErrorDetail(string? Title, string? Detail);

    public record ErrorMessageRequest(Guid Id, ConnectionId? ConnectionId, ErrorDetail Error, RetryPolicy? RetryPolicy)
    {
        public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }
}
