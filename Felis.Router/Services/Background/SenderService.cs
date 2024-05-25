﻿using Felis.Router.Abstractions;
using Felis.Router.Entities;
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
        private readonly QueueService _queueService;

        public SenderService(IRouterStorage routerStorage, ILogger<SenderService> logger, IHubContext<RouterHub> hubContext, LoadBalancingService loadBalancingService, QueueService queueService)
        {
            _routerStorage = routerStorage ?? throw new ArgumentNullException(nameof(routerStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _loadBalancingService = loadBalancingService ?? throw new ArgumentNullException(nameof(loadBalancingService));
            _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
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
                        var queueItem = _queueService.Dequeue();

                        if (queueItem == null || queueItem.MessageId == Guid.Empty) continue;

                        var messageId = queueItem.MessageId;

                        var message = _routerStorage.MessageGet(messageId);

                        if(message == null)
                        {
                            _logger.LogWarning($"Cannot find message {messageId} in messages. No processing will be done.");
                            continue;
                        }

                        if (message.Status != MessageStatus.Ready.ToString())
                        {
                            _logger.LogWarning($"Message {messageId} has status {message.Status}. No processing will be done.");
                            continue;
                        }

                        var topic = message.Header?.Topic;

                        if (string.IsNullOrWhiteSpace(topic))
                        {
                            _logger.LogWarning($"No topic for {messageId} in messages. No processing will be done.");
                            continue;
                        }

                        _logger.LogInformation($"Sending message {messageId} for topic {topic}");

                        var connectionId = _loadBalancingService.GetNextConnectionId(topic);

                        if (connectionId == null || string.IsNullOrWhiteSpace(connectionId))
                        {
                            _logger.LogWarning($"No connectionId available for topic {topic}");
                            continue;
                        }
                        
                        await _hubContext.Clients.Client(connectionId).SendAsync(topic, message, stoppingToken);

                        var messageSentSet = _routerStorage.SentMessageAdd(messageId);

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
