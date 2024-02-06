using Felis.Core.Models;

namespace Felis.Core;

public record Message(Guid Id, Header? Header, Content? Content);

public record Header(Topic? Topic, List<Service>? Services)
{ 
    public long Timestamp => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}

public record Content(string? Json);