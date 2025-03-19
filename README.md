# ![Alt text](Felis.jpg)

A light-weight message queue totally written in .NET.

The **Felis** project contains the logic for dispatching, storing and validating messages.
It stores the messages in a **ZoneTree** storage.

**Requirements**

- [.NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/overview)
- [MessagePack](https://msgpack.org/)
- [ZoneTree](https://github.com/koculu/ZoneTree)

**Dependencies**

- Microsoft.Extensions.Hosting.Abstractions 8.0.1
- MessagePack 3.1.3
- ZoneTree 1.8.5

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
    .AddBroker()
     .ConfigureServices((_, services) =>
     {
         services.AddHostedService<Subscriber>();
         services.AddHostedService<Publisher>();
     });

var host = builder.Build();

await host.RunAsync();
```

The example above initialize the **Felis Broker** in a console application, with console logging provider using the method **AddBroker**.

**Message entity**

This entity is the representation of the data available on a specific queue.

The **Message entity** is made of:

| Property   | Type   | Context                                                   |
|------------|--------|-----------------------------------------------------------|
| Id         | guid   | the message unique id assigned by the broker.             |
| Queue      | string | the queue where the message has been published.           |
| Payload    | string | the actual content of the message published on the queue. |
| Timestamp  | number | the timestamp of the message when it was published.       |
| Expiration | number | the message's expiration timestamp. It can be null.       |

**Publish of a message to a queue**

You can inject the **MessageBroker** class to make a publishing to a specific queue with the **Publish** method.

```
var messageGuid = _messageBroker.Publish("test",  $"test at {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()}", 20);
_logger.LogInformation($"Published {messageGuid}@test");

```

In the example above a message is published for the queue "test", with a string payload, with a time-to-live of 20 seconds.

****Parameters****

| Property | Type   | Context                                                                                 |
|----------|--------|-----------------------------------------------------------------------------------------|
| queue    | string | The queue where to publish the message.                                                 |
| payload  | string | The payload to publish.                                                                 |
| ttl      | int    | How many seconds a message can live. If less or equal than zero the message is durable. |

In case of success it will return the message guid, otherwise the exception mapped in the summary.

**Subscribe to a queue with Subscribe**

This method is used to subscribe to a queue. The **Subscribe** method returns an IAsyncEnumerable to stream data.

```

try
{
    await foreach (var message in _messageBroker.Subscribe("test", false, stoppingToken))
    {
        _logger.LogDebug(
            $"Received message: {JsonSerializer.Serialize(message)}");
    }
}
catch (Exception ex)
{
    _logger.LogError(ex, ex.Message);
}
```
The example above listens for messages available at "test" queue.
The subscription above is not marked as exclusive, so in case of a message published in queue mode a load
balancing of subscribers will be adopted by Felis.

***Parameters***

| Property  | Type    | Context                                |
|-----------|---------|----------------------------------------|
| queue     | string  | the queue to subscribe to.             |
| exclusive | boolean | if the subscriber is exclusive or not. |

****Response****

It returns a stream of **MessageModel**.

**How can I test it?**

This repository provides a test project where you can find tests for **MessageBroker**.

The project name is **Felis.Tests**.
