namespace Felis.Common.Models;

public record Consumer(string? Hostname, string? IpAddress, List<string> Topics, bool Unique)
{
    public List<string> Topics { get; set; } = Topics;
}

