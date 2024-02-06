namespace Felis.Core.Models;

public record Message(Header? Header, Content? Content);

public record Header(Guid Id, Topic? Topic)
{ 
    public long Timestamp => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}

public record Content(string? Json);