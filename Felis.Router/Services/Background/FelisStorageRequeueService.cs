using Felis.Router.Configurations;
using Felis.Router.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Felis.Router.Services.Background;

internal sealed class FelisStorageRequeueService : BackgroundService
{
    private readonly FelisRouterStorage _felisRouterStorage;
    private readonly ILogger<FelisStorageRequeueService> _logger;
    private readonly FelisRouterConfiguration _configuration;
    private readonly FelisRouterService _felisRouterService;

    public FelisStorageRequeueService(FelisRouterStorage felisRouterStorage, ILogger<FelisStorageRequeueService> logger, IOptionsMonitor<FelisRouterConfiguration> configuration, FelisRouterService felisRouterService)
    {
        _felisRouterStorage = felisRouterStorage ?? throw new ArgumentNullException(nameof(felisRouterStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration.CurrentValue ?? throw new ArgumentNullException(nameof(configuration));
        _felisRouterService = felisRouterService ?? throw new ArgumentNullException(nameof(felisRouterService));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var minutesForRequeue = _configuration.MessageConfiguration?.MinutesForEveryRequeue;

            if (!minutesForRequeue.HasValue || minutesForRequeue <= 0)
            {
                _logger.LogWarning(
                    "MinutesForEveryRequeue not correctly configured. The FelisStorageRequeueService won't process.");
                return;
            }

            var timer = new PeriodicTimer(
                TimeSpan.FromMinutes(minutesForRequeue.Value));
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    _logger.LogInformation("Start FelisStorageRequeueService ExecuteAsync");

                    if (_configuration?.MessageConfiguration?.TimeToLiveMinutes is not > 0) continue;
                    
                    var result = _felisRouterStorage.ListMessagesToRequeue();

                    if (!result.Any())
                    {
                        _logger.LogWarning("ListMessagesToRequeue returned empty list. No messages will be requeued.");
                        return;
                    }

                    foreach (var errorMessage in result)
                    {
                        var dispatchResult = await _felisRouterService.Dispatch(errorMessage.Message?.Header?.Topic, errorMessage.Message, stoppingToken).ConfigureAwait(false);
                        
                        _logger.LogInformation($"{(dispatchResult ? "Dispatched" : "Not dispatched")} message for Topic {errorMessage.Message?.Header?.Topic}");
                    }
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                finally
                {
                    _logger.LogInformation("End FelisStorageRequeueService ExecuteAsync");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        finally
        {
            _logger.LogInformation("Shutdown FelisStorageRequeueService");
        }
    }
}