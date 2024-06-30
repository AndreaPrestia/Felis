using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

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

    public string? GetNextConnectionId(string queue)
    {
        var connectionEntities = _connectionService.GetConnectionIdsByQueueName(queue);
        
        if (!connectionEntities.Any())
        {
            _logger.LogInformation($"No consumers found for queue {queue}");
            return null;
        }

        var uniqueConsumer = connectionEntities.Where(x => x.Subscriber.Queues.Any(q => q.Unique)).MinBy(x => x.Timestamp);

        if (uniqueConsumer != null)
        {
            _logger.LogInformation($"Found unique consumer {uniqueConsumer.ConnectionId} for queue {queue}");
            return uniqueConsumer.ConnectionId;
        }
        
        if (!_currentIndexDictionary.ContainsKey(queue))
        {
            var added = _currentIndexDictionary.TryAdd(queue, 0);
           
            _logger.LogDebug($"Index for queue {queue} added {added}");
        }

        var currentIndex = _currentIndexDictionary[queue];
        
        var connectionEntity = connectionEntities.ElementAt(currentIndex);

        var updatedIndex = (currentIndex + 1) % connectionEntities.Count;

        var updated = _currentIndexDictionary.TryUpdate(queue, updatedIndex, currentIndex);
        
        _logger.LogDebug($"Index for connectionId to use at the next run for queue {queue} is {currentIndex} updated {updated}");
        
        return connectionEntity.ConnectionId;
    }
}