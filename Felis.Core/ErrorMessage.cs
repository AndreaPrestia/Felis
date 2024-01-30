using Felis.Core.Models;

namespace Felis.Core
{
	public record ErrorMessage(Message? Message, Service? Service, Exception? Exception, RetryPolicy? RetryPolicy)
	{
		public Guid Id { get; } = Guid.NewGuid();
		public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
	}
}
