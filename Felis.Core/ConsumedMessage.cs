using Felis.Core.Models;
using System.Text.Json.Serialization;

namespace Felis.Core;

public record ConsumedMessage
{
    [JsonConstructor]
    public ConsumedMessage()
    {
        
    }

    public ConsumedMessage(Message? message, Service? service)
    {
        Message = message;
        Service = service;
    }
    
    public Message? Message { get; set; }
    public Service? Service { get; set; }
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}