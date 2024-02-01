namespace Felis.Core.Models;

public record Service(string? Name, string? Host, bool IsPublic, List<Topic> Topics)
{
    public List<Topic> Topics { get; set; } = Topics;
};

