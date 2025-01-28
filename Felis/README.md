# ![Alt text](../Felis.jpg)

A light-weight message broker totally written in .NET.

The **Felis** project contains the logic for dispatching, storing and validating messages.
It stores the messages in a **LiteDB** database.
It behaves as message queue and broadcaster if a specific message for a topic is tagged with broadcast property.

**Requirements**

- [.NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/overview)
- [LiteDB](https://www.litedb.org/)
- [JSON](https://docs.foursquare.com/analytics-products/docs/data-formats-json)

**Dependencies**

- LiteDB 5.0.21

**Usage of Broker**

Code example:

```
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Debug);
    })
    .AddFelisBroker()
     .ConfigureServices((_, services) =>
     {
         services.AddHostedService<Subscriber>();
         services.AddHostedService<Publisher>();
     });

var host = builder.Build();

await host.RunAsync();
```
The example above initialize the **Felis Broker** in a console application, with console logging provider.

The **AddFelisBroker** method takes **heartBeatInSeconds**.

**Message entity**

The JSON below represent the **Message entity** coming from the broker.

```
{
    "id": "ac4625da-e922-4c2b-a7e7-aef21ece963c",
    "topic": "test",
    "payload": "{\"description\":\"Test\"}",
    "timestamp": 1724421633359,
    "expiration": 1724421644459,
    "broadcast": true
}
```

The **Message entity** is made of:

Property | Type   | Context                                                              |
--- |--------|----------------------------------------------------------------------|
id | guid   | the message unique id assigned by the broker.                        |
topic | string | the topic where the message has been published.                      |
payload | string | the actual content of the message published on the topic.            |
timestamp | number | the timestamp of the message when it was published.                  |
expiration | number | the message's expiration timestamp. It can be null.                  |
broadcast | bool   | the message's behaviour, if it's a broadcast message or a queue one. |

**Publish of a message to a topic with Publish**

You can inject the **MessageBroker** class to make a publish to a specific topic with the **Publish** method.

```
    var messageGuid = _messageBroker.Publish("test",  $"test at {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()}", null, false);
    _logger.LogInformation($"Published {messageGuid}@test");

```

In the example above a message is published for the topic "test", with a string payload, without a time-to-live and without an exclusive consumer.

****Parameters****

Property | Type    | Context                                                                                                                                                                                |
--- |---------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
topic | string  | The topic where to publish the message.                                                                                                                                                |
payload | string  | The payload to publish.                                                                                                                                                                |
ttl | int     | How many seconds a message can live. If not specified (or 0 value is used) the message is durable.                                                                                     |
broadcast | boolean | Tells if the message must be broadcasted to all subscribers. If not provided or set to false Felis broker will send enqueued message only to one subscriber in a load balanced manner. |

In case of success it will return the message guid, otherwise the exception mapped in the summary.

**Subscribe to a topic with Subscribe**

This method is used to subscribe to a topic using channels to stream structured data.

```
        var subscription = _messageBroker.Subscribe("test", null);

        try
        {
            await foreach (var message in subscription.MessageChannel.Reader.ReadAllAsync(stoppingToken))
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
```
The example above create a subscription entity and listen for message available at "test" topic.
The subscriber above is not marked as exclusive, so in case of a message published in queue mode a load 
balancing of subscribers will be adopted by Felis.

***Parameters***

Property | Type    | Context                                |
--- |---------|----------------------------------------|
topic | string  | the topic to subscribe to.             |
exclusive | boolean | if the subscriber is exclusive or not. |

****Response****

It returns a subscription entity that exposes a **System.Threading.Channel<MessageModel>** property called **MessageChannel**.

**How can I test it?**

This repository provides the examples of usage:

- **Felis.Broker.Standalone.Console**

**Felis.Broker.Standalone.Console**

A console application, that reference Felis project and uses the broker as in-process flow.