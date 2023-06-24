using Felis.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Felis.Core;

public record Message
{
    public Topic? Topic { get; set; }
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    public string? Content { get; set; }
    public string? Type { get; set; }
    
    public List<string>? ServiceHosts { get; set; }

    [JsonConstructor]
    public Message()
    {
        
    }
    
    public Message(Topic? topic, object content, string? type)
    {
        Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        Content = JsonSerializer.Serialize(content) ?? throw new ArgumentNullException(nameof(content));
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }
    
    public Message(Topic? topic, object content, string? type, List<string>? serviceHosts)
    {
        Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        Content = JsonSerializer.Serialize(content) ?? throw new ArgumentNullException(nameof(content));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        ServiceHosts = serviceHosts ?? throw new ArgumentNullException(nameof(serviceHosts));
    }
}