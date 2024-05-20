﻿namespace Felis.Core.Models;

public record Message(Header? Header, Content? Content);

public record Header(Guid Id, string Topic, long Timestamp)
{ 
    public long Timestamp => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}

public record Content(string? Payload);