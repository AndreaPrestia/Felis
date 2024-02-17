using Felis.LoadBalancer.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Felis.LoadBalancer.Service;

internal sealed class LoadBalancingService
{
    private readonly ILogger<LoadBalancingService> _logger;
    private readonly IOptionsMonitor<LoadBalancerConfiguration> _loadBalancerConfiguration;
    private int _currentIndex = 0;

    public LoadBalancingService(ILogger<LoadBalancingService> logger,
        IOptionsMonitor<LoadBalancerConfiguration> loadBalancerConfiguration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loadBalancerConfiguration =
            loadBalancerConfiguration ?? throw new ArgumentNullException(nameof(loadBalancerConfiguration));
    }

    public string? GetNextRouterEndpoint()
    {
        var routers = _loadBalancerConfiguration.CurrentValue.Routers;

        if (routers.Count == 0)
        {
            _logger.LogInformation($"No Routers set in {LoadBalancerConfiguration.FelisLoadBalancer}");
            return null;
        }

        var router = routers.ElementAt(_currentIndex);

        _logger.LogDebug($"Router {router} with index {_currentIndex}");

        _currentIndex = (_currentIndex + 1) % routers.Count;

        _logger.LogDebug(
            $"Index for router to use at the next run is {_currentIndex}");

        return router;
    }
}