using System.Text.Json;
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
        var subscription = _messageBroker.Subscribe("test", null);

        try
        {
            await foreach (var message in subscription.GetNextAvailableMessageAsync(stoppingToken))
            {
                _logger.LogDebug(
                    $"Received message for subscriber {subscription.Id} - test: {JsonSerializer.Serialize(message)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        finally
        {
            _messageBroker.UnSubscribe("test", subscription);
        }
    }
}