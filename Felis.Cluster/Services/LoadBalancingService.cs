using Felis.Cluster.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Felis.Cluster.Services;

public sealed class LoadBalancingService
{
	private readonly ILogger<LoadBalancingService> _logger;
    private List<string>? _routers;
    private readonly object _lockObject = new();

    public LoadBalancingService(ILogger<LoadBalancingService> logger,
		IOptionsMonitor<LoadBalancerConfiguration> optionsMonitor)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

        UpdateServers(optionsMonitor.CurrentValue);

        optionsMonitor.OnChange((config, _) =>
        {
            UpdateServers(config);
        });
    }

    /// <summary>
    /// Sticky session
    /// </summary>
    /// <param name="sessionId"></param>
    /// <returns></returns>
	public string? GetNextRouterEndpoint(string? sessionId)
	{
        if (sessionId == null)
        {
            _logger.LogWarning("No sessionId provided.");
            return null;
        }

        lock (_lockObject)
        {

            if (_routers == null || _routers.Count == 0)
            {
                _logger.LogInformation($"No Routers set in {LoadBalancerConfiguration.FelisLoadBalancer}");
                return null;
            }

            var hash = sessionId.GetHashCode();
            var index = Math.Abs(hash) % _routers.Count;

            var router = _routers.ElementAt(index);

            if (string.IsNullOrWhiteSpace(router))
            {
                _logger.LogWarning($"No server found at index {index}");
                return null;
            }

            _logger.LogDebug(
                $"Router {router} for session {sessionId}");

            return router;
        }
    }

    private void UpdateServers(LoadBalancerConfiguration? configuration)
    {
        if (configuration == null || configuration.Routers.Count == 0)
        {
            _logger.LogDebug("Routers from configuration not provided");
            return;
        }

        lock (_lockObject)
        {
            _routers ??= new List<string>();
            _routers.Clear();

            _routers = configuration.Routers;
        }
    }
}