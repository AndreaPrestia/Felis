using Felis.Router.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Services.Background;

internal sealed class RequeueService : BackgroundService
{
    private readonly IRouterStorage _routerStorage;
    private readonly ILogger<RequeueService> _logger;
    private readonly RouterService _routerService;

    public RequeueService(IRouterStorage routerStorage, ILogger<RequeueService> logger, RouterService routerService)
    {
        _routerStorage = routerStorage ?? throw new ArgumentNullException(nameof(routerStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _routerService = routerService ?? throw new ArgumentNullException(nameof(routerService));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var timer = new PeriodicTimer(
                TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation("Start FelisStorageRequeueService ExecuteAsync");

                    var errorMessage = _routerStorage.ErrorMessageGet();

                    if (errorMessage == null)
                    {
                        _logger.LogWarning("No error message to requeue. No messages will be requeued.");
                        continue;
                    }
 
                    var dispatchResult = _routerService.Dispatch(errorMessage.Message.Header?.Topic, errorMessage.Message);

                    _logger.LogInformation(
                        $"{(dispatchResult ? "Dispatched" : "Not dispatched")} message for Topic {errorMessage.Message.Header?.Topic}");
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