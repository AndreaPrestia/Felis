using System.Text.Json;
using System.Text.Json.Serialization;

namespace Felis.Core;

public record Message
{
    public string Topic { get; set; }
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    public string Content { get; set; }

    [JsonConstructor]
    public Message()
    {
        
    }
    
    public Message(string? topic, object content)
    {
        Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        Content = JsonSerializer.Serialize(content) ?? throw new ArgumentNullException(nameof(content));
    }
}