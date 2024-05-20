namespace Felis.Core.Models;

public record Consumer(string FriendlyName, string? Hostname, string? IpAddress, List<string> Topics)
{
    public List<string> Topics { get; set; } = Topics;
}

