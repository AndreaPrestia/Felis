using System.Drawing;
using Felis.Core.Models;
using System.Text.Json.Serialization;

namespace Felis.Core
{
	public record ErrorMessage
	{
		[JsonConstructor]
		public ErrorMessage()
		{

		}

		public ErrorMessage(Message? message, Service service, Exception? exception)
		{
			Message = message;
			Service = service;
			Exception = exception;
		}

		public Message? Message { get; set; }
		public Service? Service { get; set; }
		public Exception? Exception { get; set; }
		public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
	}
}
