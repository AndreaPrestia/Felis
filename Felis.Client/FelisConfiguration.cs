using Felis.Core;

namespace Felis.Client;

public record FelisConfiguration
{
    public const string FelisClient = nameof(FelisClient);
    public FelisConfigurationRouter? Router { get; set; }
    public RetryPolicy? RetryPolicy { get; set; }
}

public record FelisConfigurationRouter
{
    public string? Endpoint { get; set; }
    public int PooledConnectionLifetimeMinutes { get; set; }
}