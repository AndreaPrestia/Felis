using Felis.Router.Abstractions;
using Felis.Router.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Services.Background
{
    internal sealed class SenderService : BackgroundService
    {
        private readonly IRouterStorage _routerStorage;
        private readonly ILogger<SenderService> _logger;
        private readonly IHubContext<RouterHub> _hubContext;
        private readonly LoadBalancingService _loadBalancingService;

        public SenderService(IRouterStorage routerStorage, ILogger<SenderService> logger, IHubContext<RouterHub> hubContext, LoadBalancingService loadBalancingService)
        {
            _routerStorage = routerStorage ?? throw new ArgumentNullException(nameof(routerStorage));
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
                        var message = _routerStorage.ReadyMessageGet();

                        if (message == null || string.IsNullOrWhiteSpace(message.Header?.Topic)) continue;

                        _logger.LogInformation($"Sending message {message.Header?.Id} for topic {message.Header?.Topic}");

                        if (message.Header?.Topic == null)
                        {
                            _logger.LogWarning($"No topic available for topic {message.Header?.Id}");
                            continue;
                        }

                        var connectionId = _loadBalancingService.GetNextConnectionId(message.Header?.Topic!);

                        if (connectionId == null || string.IsNullOrWhiteSpace(connectionId))
                        {
                            _logger.LogWarning($"No connectionId available for topic {message.Header?.Topic}");
                            continue;
                        }
                        
                        await _hubContext.Clients.Client(connectionId).SendAsync(message.Header?.Topic!, message, stoppingToken);

                        var messageSentSet = _routerStorage.SentMessageAdd(message);

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
