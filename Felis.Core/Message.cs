using System.Security.Cryptography;
using Felis.Core.Models;
using System.Text.Json.Serialization;

namespace Felis.Core;

public record Message(Header? Header, Content? Content);

public record Header(Topic? Topic, List<Service>? Services)
{
    public long Timestamp => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
}

public record Content(string? Json);