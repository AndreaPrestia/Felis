namespace Felis.Core.Models;

public record Message(Header? Header, Content? Content);

public record Header(Guid Id, string Topic, long Timestamp)
{ 
    public Header(Guid Id, string Topic) : this(Id, Topic, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds())
    {
    }
}

public record Content(string? Payload);