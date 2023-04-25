using Microsoft.Extensions.Configuration;

namespace Felis.Core;

public record FelisConfiguration
{
    public string? RouterEndpoint { get; set; }
}