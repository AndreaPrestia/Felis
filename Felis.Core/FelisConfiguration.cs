using Felis.Core.Models;
using Microsoft.Extensions.Configuration;

namespace Felis.Core;

public record FelisConfiguration
{
    public string? RouterEndpoint { get; set; }
    public Service? Service { get; set; }
}