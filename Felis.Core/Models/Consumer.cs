namespace Felis.Core.Models;

public record Consumer(string FriendlyName, string? Hostname, string? IpAddress, List<Topic> Topics)
{
    public List<Topic> Topics { get; set; } = Topics;
}

