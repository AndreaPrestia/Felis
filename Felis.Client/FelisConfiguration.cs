﻿using Felis.Core.Models;

namespace Felis.Client;

public record FelisConfiguration
{
    public FelisConfigurationRouter? Router { get; set; }
    public Service? Service { get; set; }
}

public record FelisConfigurationRouter
{
    public string? Endpoint { get; set; }
}