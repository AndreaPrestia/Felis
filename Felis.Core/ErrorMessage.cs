using Felis.Core.Models;
using System.Text.Json.Serialization;

namespace Felis.Core
{
	public class ErrorMessage
	{
		[JsonConstructor]
		public ErrorMessage()
		{

		}

		public ErrorMessage(Message? message, Client client, Exception? exception)
		{
			Message = message;
			Client = client;
			Exception = exception;
		}

		public Message? Message { get; set; }
		public Client? Client { get; set; }
		public Exception? Exception { get; set; }
		public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
	}
}
