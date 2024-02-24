using System.Collections.Concurrent;
using Felis.Cluster.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Felis.Cluster.Services;

public sealed class LoadBalancingService
{
	private readonly ILogger<LoadBalancingService> _logger;
	private ConcurrentBag<string> _routers;
	private int _currentIndex;
	public List<string> CurrentRouters => _routers.ToList();

	public LoadBalancingService(ILogger<LoadBalancingService> logger,
		IOptionsMonitor<LoadBalancerConfiguration> loadBalancerConfiguration)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_routers =
			new ConcurrentBag<string>(loadBalancerConfiguration.CurrentValue.Routers) ?? throw new ArgumentNullException(nameof(loadBalancerConfiguration.CurrentValue));
		loadBalancerConfiguration.OnChange((config, _) =>
		{
			_currentIndex = 0;
			_routers = new ConcurrentBag<string>(config.Routers);
		});
	}

	public string? GetNextRouterEndpoint()
	{
		if (_routers.Count == 0)
		{
			_logger.LogInformation($"No Routers set in {LoadBalancerConfiguration.FelisLoadBalancer}");
			return null;
		}

		var router = _routers.ElementAt(_currentIndex);

		_logger.LogDebug($"Router {router} with index {_currentIndex}");

		_currentIndex = (_currentIndex + 1) % _routers.Count;

		_logger.LogDebug(
			$"Index for router to use at the next run is {_currentIndex}");

		return router;
	}
}