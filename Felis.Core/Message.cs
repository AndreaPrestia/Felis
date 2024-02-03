using Felis.Core.Models;

namespace Felis.Core;

public record Message(Header? Header, Content? Content)
{
    public Guid Id { get; } = Guid.NewGuid();
}

public record Header(Topic? Topic, List<Service>? Services)
{ 
    public long Timestamp => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}

public record Content(string? Json);