using Felis.Core.Models;
using System.Text.Json.Serialization;

namespace Felis.Core;

public record ConsumedMessage
{
    [JsonConstructor]
    public ConsumedMessage()
    {
        
    }

    public ConsumedMessage(Message? message, Client client)
    {
        Message = message;
        Client = client;
    }
    
    public Message? Message { get; set; }
    public Client? Client { get; set; }
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}