namespace Felis.Core
{
	public record ErrorMessage(Message? Message, ConnectionId? ConnectionId, ErrorDetail? Error, RetryPolicy? RetryPolicy)
	{
		public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
	}

	public record ErrorDetail(string? Title, string? Detail);
}
