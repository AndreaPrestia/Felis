namespace Felis.Core.Models;

public record Message(Header? Header, Content? Content);

public record Header(Guid Id, Topic? Topic, Origin? Origin)
{ 
    public long Timestamp => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    public Origin? Origin { get; set; } = null;
}

public record Origin(string? Hostname, string? IpAddress);

public record Content(string? Json);