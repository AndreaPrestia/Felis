using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Felis.Broker.Standalone.Console;

public class Publisher : BackgroundService
{
    private readonly MessageBroker _messageBroker;
    private readonly ILogger<Publisher> _logger;

    public Publisher(MessageBroker messageBroker, ILogger<Publisher> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var messageGuid = _messageBroker.Publish("test",  $"test at {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()}");
            _logger.LogInformation($"Published {messageGuid}@test");
            
            Thread.Sleep(5000);
        }

        return Task.CompletedTask;
    }
}