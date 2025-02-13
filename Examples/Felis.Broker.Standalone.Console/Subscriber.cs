using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Felis.Broker.Standalone.Console;

public class Subscriber : BackgroundService
{
    private readonly MessageBroker _messageBroker;
    private readonly ILogger<Subscriber> _logger;

    public Subscriber(MessageBroker messageBroker, ILogger<Subscriber> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var taskGeneric =
                    SubscribeInParallelAsync(20, "Generic", false, stoppingToken);
                var taskExclusive =
                    SubscribeInParallelAsync(1, "Exclusive", true, stoppingToken);
                await Task.WhenAll(new[] { taskGeneric, taskExclusive });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in subscriber: '{0}'", ex.Message);
            }
            finally
            {
                _logger.LogInformation("Terminated subscriber");
            }
        }

        _logger.LogInformation("Terminated subscriber");
    }

    private async Task SubscribeInParallelAsync(int numberOfSubscribers, string queue, bool exclusive,
        CancellationToken cancellationToken)
    {
        try
        {
            var subscriberTasks = new Task[numberOfSubscribers];
            for (var i = 0; i < numberOfSubscribers; i++)
            {
                var subscriberId = i + 1;
                subscriberTasks[i] = Task.Run(() =>
                    SubscribeAsync(subscriberId, queue, exclusive, cancellationToken));
            }

            await Task.WhenAll(subscriberTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in subscriber: '{0}'", ex.Message);
        }
    }

    private async Task SubscribeAsync(int subscriberId, string queue, bool exclusive,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _messageBroker.Subscribe(queue, exclusive, cancellationToken))
            {
                _logger.LogInformation($"Received message {message?.Id} for subscriber {subscriberId} - {queue}: {message?.Payload}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
}