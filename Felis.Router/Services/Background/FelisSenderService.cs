using Felis.Router.Hubs;
using Felis.Router.Storage;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Services.Background
{
    internal sealed class FelisSenderService : BackgroundService
    {
        private readonly FelisRouterStorage _felisRouterStorage;
        private readonly ILogger<FelisSenderService> _logger;
        private readonly IHubContext<FelisRouterHub> _hubContext;

        public FelisSenderService(FelisRouterStorage felisRouterStorage, ILogger<FelisSenderService> logger, IHubContext<FelisRouterHub> hubContext)
        {
            _felisRouterStorage = felisRouterStorage ?? throw new ArgumentNullException(nameof(felisRouterStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
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

                        if (message == null || string.IsNullOrWhiteSpace(message.Header?.Topic?.Value)) continue;

                        _logger.LogInformation($"Sending message {message.Header?.Id} for topic {message.Header?.Topic?.Value}");

                        //TODO inject connection manager and choose service by load balancing
                        await _hubContext.Clients.All.SendAsync(message.Header?.Topic?.Value!, message, stoppingToken);

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
