using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace Felis.Mqtt;

internal class MqttServerLifetimeService : IHostedService
{
    private readonly MqttServer _mqttServer;
    private readonly MessageBroker _messageBroker;
    private readonly ILogger<MqttServerLifetimeService> _logger;

    public MqttServerLifetimeService(MqttServer mqttServer, MessageBroker messageBroker,
        ILogger<MqttServerLifetimeService> logger)
    {
        _mqttServer = mqttServer;
        _messageBroker = messageBroker;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // No specific action needed on start
        _mqttServer.ValidatingConnectionAsync += args =>
        {
            args.ReasonCode = args.ClientId.Length < 10
                ? MqttConnectReasonCode.ClientIdentifierNotValid
                : MqttConnectReasonCode.Success;
            return Task.CompletedTask;
        };

        _mqttServer.ClientSubscribedTopicAsync += async args =>
        {
            var subscriptionEntity =
                _messageBroker.Subscribe(args.TopicFilter.Topic,
                    bool.Parse(args.SessionItems["exclusive"]?.ToString() ?? "false"));

            try
            {
                _logger.LogInformation("Subscribed {clientId} with id {subscriptionId}", args.ClientId,
                    subscriptionEntity.Id);

                await foreach (var message in subscriptionEntity.MessageChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    var applicationMessage = new MqttApplicationMessageBuilder()
                        .WithTopic(message.Topic)
                        .WithPayload(JsonSerializer.Serialize(message))
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithRetainFlag(false) 
                        .Build();
                    
                    await _mqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(applicationMessage), cancellationToken);

                    _logger.LogInformation($"Message published to topic: {message.Topic}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                _messageBroker.UnSubscribe(args.TopicFilter.Topic, subscriptionEntity);
            }
        };
        
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _mqttServer.StopAsync();
    }
}