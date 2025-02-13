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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var taskGeneric = PublishInParallelAsync(20, "Generic");
            var taskExclusive = PublishInParallelAsync(20, "Exclusive");
            await Task.WhenAll(new [] {taskGeneric, taskExclusive});
            _logger.LogInformation("Publish finished, waiting 5 seconds to next round");
            Thread.Sleep(5000);
        }
    }
    
    private async Task PublishInParallelAsync(int numberOfPublishers, string queue)
    {
        try
        {
            var publisherTasks = new Task[numberOfPublishers];
            for (var i = 0; i < numberOfPublishers; i++)
            {
                publisherTasks[i] = Task.Run(() =>
                {
                    var message = _messageBroker.Publish(queue,  $"{queue} at {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()}");
                    _logger.LogInformation($"Published {message.Id}@{queue}");
                });
            }

            await Task.WhenAll(publisherTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in publisher: '{0}'", ex.Message);
        }
    }
}