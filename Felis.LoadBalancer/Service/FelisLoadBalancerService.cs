using Felis.LoadBalancer.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Felis.LoadBalancer.Service;

internal sealed class FelisLoadBalancerService
{
    private readonly ILogger<FelisLoadBalancerService> _logger;
    private readonly IOptionsMonitor<FelisLoadBalancerConfiguration> _felisLoadBalancerConfiguration;
    private int _currentIndex = 0;

    public FelisLoadBalancerService(ILogger<FelisLoadBalancerService> logger,
        IOptionsMonitor<FelisLoadBalancerConfiguration> felisLoadBalancerConfiguration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _felisLoadBalancerConfiguration =
            felisLoadBalancerConfiguration ?? throw new ArgumentNullException(nameof(felisLoadBalancerConfiguration));
    }

    public string? GetNextRouterEndpoint()
    {
        var routers = _felisLoadBalancerConfiguration.CurrentValue.Routers;

        if (routers.Count == 0)
        {
            _logger.LogInformation($"No Routers set in {FelisLoadBalancerConfiguration.FelisLoadBalancer}");
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