using Felis.Router.Hubs;
using Felis.Router.Storage;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Services.Background
{
    internal sealed class SenderService : BackgroundService
    {
        private readonly RouterStorage _felisRouterStorage;
        private readonly ILogger<SenderService> _logger;
        private readonly IHubContext<RouterHub> _hubContext;
        private readonly LoadBalancingService _loadBalancingService;

        public SenderService(RouterStorage felisRouterStorage, ILogger<SenderService> logger, IHubContext<RouterHub> hubContext, LoadBalancingService loadBalancingService)
        {
            _felisRouterStorage = felisRouterStorage ?? throw new ArgumentNullException(nameof(felisRouterStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
			_loadBalancingService = loadBalancingService ?? throw new ArgumentNullException(nameof(loadBalancingService));
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
                        var message = _felisRouterStorage.ReadyMessageGet();

                        if (message == null || message.Header?.Topic == null || string.IsNullOrWhiteSpace(message.Header?.Topic?.Value)) continue;

                        _logger.LogInformation($"Sending message {message.Header?.Id} for topic {message.Header?.Topic?.Value}");

                        var connectionId = _loadBalancingService.GetNextConnectionId(message.Header?.Topic!);

                        if (connectionId == null || string.IsNullOrWhiteSpace(connectionId.Value))
                        {
                            _logger.LogWarning($"No connectionId available for topic {message.Header?.Topic?.Value}");
                            continue;
                        }

                        await _hubContext.Clients.Client(connectionId.Value).SendAsync(message.Header?.Topic?.Value!, message, stoppingToken);

                        var messageSentSet = _felisRouterStorage.SentMessageAdd(message);

                        _logger.LogWarning($"Message {message.Header?.Id} sent {messageSentSet}.");
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
