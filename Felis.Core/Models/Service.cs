namespace Felis.Core.Models;

public record Service(string? Hostname, string? IpAddress, List<Topic> Topics)
{
    public List<Topic> Topics { get; set; } = Topics;
};

