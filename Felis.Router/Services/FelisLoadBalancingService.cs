using System.Collections.Concurrent;
using Felis.Core.Models;
using Felis.Router.Configurations;
using Felis.Router.Managers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Felis.Router.Services;

internal class FelisLoadBalancingService
{
    private readonly ILogger<FelisLoadBalancingService> _logger;
    private readonly FelisConnectionManager _felisConnectionManager;
    private ConcurrentDictionary<Topic, int> _currentIndexDictionary = new();
    private readonly IOptionsMonitor<LoadBalancingConfiguration> _loadBalancingOptionsMonitorConfiguration;
    private readonly HubConnection? _hubConnection;
    private readonly FelisRouterService _felisRouterService;

    public FelisLoadBalancingService(ILogger<FelisLoadBalancingService> logger,
        FelisConnectionManager felisConnectionManager,
        IOptionsMonitor<LoadBalancingConfiguration> loadBalancingOptionsMonitorConfiguration,
        HubConnection hubConnection, FelisRouterService felisRouterService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _felisConnectionManager =
            felisConnectionManager ?? throw new ArgumentNullException(nameof(felisConnectionManager));
        _loadBalancingOptionsMonitorConfiguration = loadBalancingOptionsMonitorConfiguration ??
                                                    throw new ArgumentNullException(
                                                        nameof(loadBalancingOptionsMonitorConfiguration));
        _hubConnection = hubConnection ?? throw new ArgumentNullException(nameof(hubConnection));
        _felisRouterService = felisRouterService ?? throw new ArgumentNullException(nameof(felisRouterService));
    }

    public async Task SubscribeToBalancerAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection == null)
        {
            throw new ArgumentNullException($"Connection to Felis balancer not correctly initialized");
        }

        _hubConnection.On<Message?>("NewMessageToDispatch", async (messageIncoming) =>
        {
            try
            {
                if (messageIncoming == null)
                {
                    _logger.LogWarning("No message incoming.");
                    return;
                }

                if (messageIncoming.Header?.Topic == null ||
                    string.IsNullOrWhiteSpace(messageIncoming.Header?.Topic?.Value))
                {
                    _logger.LogWarning("No Topic provided in Header.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(_hubConnection?.ConnectionId))
                {
                    _logger.LogWarning("No connection id found. No message will be processed.");
                    return;
                }

                var dispatched = await _felisRouterService.Dispatch(messageIncoming.Header.Topic, messageIncoming);

                _logger.LogInformation($"Message {messageIncoming.Header.Id} dispatched {dispatched}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        });

        await CheckHubConnectionStateAndStartIt(cancellationToken);
    }

    public ConnectionId? GetNextConnectionId(Topic topic)
    {
        var connectionIds = _felisConnectionManager.GetConnectionIds(topic);

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

        _logger.LogDebug(
            $"Index for connectionId to use at the next run for topic {topic.Value} is {currentIndex} updated {updated}");

        return connectionId;
    }

    #region PrivateMethods

    private async Task CheckHubConnectionStateAndStartIt(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_hubConnection == null)
            {
                throw new ArgumentNullException(nameof(_hubConnection));
            }

            if (_hubConnection?.State == HubConnectionState.Disconnected)
            {
                await _hubConnection?.StartAsync(cancellationToken)!;
            }

            await _hubConnection?.InvokeAsync("SetConnectionId", cancellationToken)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    #endregion
}