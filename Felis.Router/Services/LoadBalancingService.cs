using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Felis.Router.Services;

internal class LoadBalancingService
{
    private readonly ILogger<LoadBalancingService> _logger;
    private readonly ConnectionService _connectionService;
    private readonly ConcurrentDictionary<string, int> _currentIndexDictionary = new();

    public LoadBalancingService(ILogger<LoadBalancingService> logger, ConnectionService connectionService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionService = connectionService ?? throw  new ArgumentNullException(nameof(connectionService));
    }

    public string? GetNextConnectionId(string topic)
    {
        var connectionEntities = _connectionService.GetConnectionIds(topic);
        
        if (!connectionEntities.Any())
        {
            _logger.LogInformation($"No consumers found for topic {topic}");
            return null;
        }

        var uniqueConsumer = connectionEntities.Where(x => x.Consumer.Unique).MinBy(x => x.Timestamp);

        if (uniqueConsumer != null)
        {
            _logger.LogInformation($"Found unique consumer {uniqueConsumer.ConnectionId} for topic {topic}");
            return uniqueConsumer.ConnectionId;
        }
        
        if (!_currentIndexDictionary.ContainsKey(topic))
        {
           var added = _currentIndexDictionary.TryAdd(topic, 0);
           
           _logger.LogDebug($"Index for topic {topic} added {added}");
        }

        var currentIndex = _currentIndexDictionary[topic];
        
        var connectionEntity = connectionEntities.ElementAt(currentIndex);

        var updatedIndex = (currentIndex + 1) % connectionEntities.Count;

        var updated = _currentIndexDictionary.TryUpdate(topic, updatedIndex, currentIndex);
        
        _logger.LogDebug($"Index for connectionId to use at the next run for topic {topic} is {currentIndex} updated {updated}");
        
        return connectionEntity.ConnectionId;
    }
}