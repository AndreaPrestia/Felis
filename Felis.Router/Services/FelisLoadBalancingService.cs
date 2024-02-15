using System.Collections.Concurrent;
using Felis.Core.Models;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Services;

public class FelisLoadBalancingService
{
    private readonly ILogger<FelisLoadBalancingService> _logger;
    private ConcurrentDictionary<Topic, ConcurrentBag<Consumer>> _servers = new();
    private int _currentIndex = 0;

    public FelisLoadBalancingService(ILogger<FelisLoadBalancingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool Set(Topic topic, List<Consumer> consumers)
    {
        if (_servers.TryGetValue(topic, out var server))
        {
            return _servers.TryUpdate(topic, new ConcurrentBag<Consumer>(consumers), server);
        }

        _servers = new ConcurrentDictionary<Topic, ConcurrentBag<Consumer>>(_servers.Append(
            new KeyValuePair<Topic, ConcurrentBag<Consumer>>(topic, new ConcurrentBag<Consumer>(consumers))));

        return true;
    }
    
    public Consumer? GetNextConsumer(Topic topic)
    {
        var hasValue = _servers.TryGetValue(topic, out var servers);

        if (!hasValue)
        {
            _logger.LogInformation($"No consumers found for topic {topic.Value}");
            return null;
        }

        if (servers == null)
        {
            _logger.LogInformation($"No servers found for topic {topic.Value}");
            return null;
        }
        
        var server = servers.ElementAt(_currentIndex);
        
        _currentIndex = (_currentIndex + 1) % servers.Count;
        
        return server;
    }
}