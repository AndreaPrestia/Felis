using Felis.Router.Managers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Services.Background
{
    internal sealed class DispatcherService : BackgroundService
    {
        private readonly ILogger<DispatcherService> _logger;
        private readonly RouterManager _routerManager;

        public DispatcherService(ILogger<DispatcherService> logger, RouterManager routerManager)
        {
            _routerManager = routerManager ?? throw new ArgumentNullException(nameof(routerManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var timer = new PeriodicTimer(
                    TimeSpan.FromSeconds(10));
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        var sendResult = await _routerManager.SendNextMessageAsync(stoppingToken);

                        _logger.LogWarning($"Message {sendResult.MessageId} sent {sendResult.MessageSendStatus.ToString()}.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                    finally
                    {
                        _logger.LogInformation("End FelisSenderService ExecuteAsync");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                _logger.LogInformation("Shutdown FelisSenderService");
            }
        }
    }
}