using System.Text.Json.Serialization;

namespace Felis.Core;

public record ConsumedMessage
{
    [JsonConstructor]
    public ConsumedMessage()
    {
        
    }

    public ConsumedMessage(Message message, Guid client)
    {
        Message = message;
        Client = client;
    }
    
    public Message Message { get; set; }
    public Guid Client { get; set; }
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}