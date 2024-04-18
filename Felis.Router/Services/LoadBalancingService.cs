﻿using System.Collections.Concurrent;
using Felis.Core.Models;
using Felis.Router.Managers;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Services;

internal class LoadBalancingService
{
    private readonly ILogger<LoadBalancingService> _logger;
    private readonly ConnectionManager _connectionManager;
    private readonly ConcurrentDictionary<Topic, int> _currentIndexDictionary = new();

    public LoadBalancingService(ILogger<LoadBalancingService> logger, ConnectionManager connectionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionManager = connectionManager ?? throw  new ArgumentNullException(nameof(connectionManager));
    }

    public ConnectionId? GetNextConnectionId(Topic topic)
    {
        var connectionIds = _connectionManager.GetConnectionIds(topic);
        
        if (!connectionIds.Any())
        {
            _logger.LogInformation($"No consumers found for topic {topic.Value}");
            return null;
        }

        if (!_currentIndexDictionary.ContainsKey(topic))
        {
           var added = _currentIndexDictionary.TryAdd(topic, 0);
           
           _logger.LogDebug($"Index for topic {topic.Value} added {added}");
        }

        var currentIndex = _currentIndexDictionary[topic];
        
        var connectionId = connectionIds.ElementAt(currentIndex);

        var updatedIndex = (currentIndex + 1) % connectionIds.Count;

        var updated = _currentIndexDictionary.TryUpdate(topic, updatedIndex, currentIndex);
        
        _logger.LogDebug($"Index for connectionId to use at the next run for topic {topic.Value} is {currentIndex} updated {updated}");
        
        return connectionId;
    }
}