﻿using Felis.Router.Abstractions;
using Felis.Router.Configurations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Felis.Router.Services.Background;

internal class CleanService : BackgroundService
{
    private readonly IRouterStorage _routerStorage;
    private readonly ILogger<CleanService> _logger;
    private readonly RouterConfiguration _configuration;

    public CleanService(IRouterStorage routerStorage, ILogger<CleanService> logger,
        IOptionsMonitor<RouterConfiguration> configuration)
    {
        _routerStorage = routerStorage ?? throw new ArgumentNullException(nameof(routerStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration.CurrentValue ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var minutesForClean = _configuration.MessageConfiguration?.MinutesForEveryClean;

            if (!minutesForClean.HasValue || minutesForClean <= 0)
            {
                _logger.LogWarning(
                    "MinutesForEveryClean not correctly configured. The FelisStorageCleanService won't process.");
                return;
            }

            var timer = new PeriodicTimer(
                TimeSpan.FromMinutes(minutesForClean.Value));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation("Start FelisStorageCleanService ExecuteAsync");

                    if (_configuration?.MessageConfiguration?.TimeToLiveMinutes is not > 0) continue;
                    
                    _logger.LogInformation(
                        $"Purging messages with TTL {_configuration.MessageConfiguration?.TimeToLiveMinutes}");
                    var result = _routerStorage.ReadyMessagePurge(_configuration.MessageConfiguration?.TimeToLiveMinutes);

                    if (!result)
                    {
                        _logger.LogWarning("ReadyMessagePurge returned false. No messages where purged.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                finally
                {
                    _logger.LogInformation("End FelisStorageCleanService ExecuteAsync");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        finally
        {
            _logger.LogInformation("Shutdown FelisStorageCleanService");
        }
    }
}