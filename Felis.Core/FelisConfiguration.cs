using Felis.Core.Models;
using Microsoft.Extensions.Configuration;

namespace Felis.Core;

public record FelisConfiguration
{
    public FelisConfigurationRouter? Router { get; set; }
    public Service? Service { get; set; }
}

public record FelisConfigurationRouter
{
    public string? Endpoint { get; set; }
}