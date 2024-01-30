﻿using Felis.Core;
using Felis.Core.Models;

namespace Felis.Client;

public record FelisConfiguration
{
    public const string FelisClient = nameof(FelisClient);
    public FelisConfigurationRouter? Router { get; set; }
    public Service? Service { get; set; }
    public RetryPolicy? RetryPolicy { get; set; }
}

public record FelisConfigurationRouter
{
    public string? Endpoint { get; set; }
    public int PooledConnectionLifetimeMinutes { get; set; }
}