# ![Alt text](Felis.jpg)

A light-weight message queue totally written in .NET.

The **Felis** project contains the logic for dispatching, storing and validating messages.
It stores the messages in a **LiteDB** database.

**Requirements**

- [.NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/overview)
- [LiteDB](https://www.litedb.org/)

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

The example above initialize the **Felis Broker** in a console application, with console logging provider using the method **AddFelisBroker**.

**Message entity**

This entity is the representation of the data available on a specific queue.

The **Message entity** is made of:

Property | Type   | Context                                                              |
--- |--------|----------------------------------------------------------------------|
Id | guid   | the message unique id assigned by the broker.                        |
Queue | string | the queue where the message has been published.                      |
Payload | string | the actual content of the message published on the queue.            |
Timestamp | number | the timestamp of the message when it was published.                  |
Expiration | number | the message's expiration timestamp. It can be null.                  |

**Publish of a message to a queue**

You can inject the **MessageBroker** class to make a publish to a specific queue with the **Publish** method.

```
var messageGuid = _messageBroker.Publish("test",  $"test at {new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()}", 20);
_logger.LogInformation($"Published {messageGuid}@test");

```

In the example above a message is published for the queue "test", with a string payload, without a time-to-live.

****Parameters****

Property | Type    | Context                                                                                 |
--- |---------|-----------------------------------------------------------------------------------------|
queue | string  | The queue where to publish the message.                                                 |
payload | string  | The payload to publish.                                                                 |
ttl | int     | How many seconds a message can live. If less or equal than zero the message is durable. |

In case of success it will return the message guid, otherwise the exception mapped in the summary.

**Subscribe to a queue with Subscribe**

This method is used to subscribe to a queue. The **Subscribe** method returns an an IAsyncEnumerable to stream data.

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

Property | Type    | Context                                |
--- |---------|----------------------------------------|
queue | string  | the queue to subscribe to.             |
exclusive | boolean | if the subscriber is exclusive or not. |

****Response****

It returns a stream of **MessageModel**.

**How can I test it?**

This repository provides the examples of usage:

- **Felis.Broker.Standalone.Console**

**Felis.Broker.Standalone.Console**

A console application, that reference Felis project and uses the broker as in-process flow.

# Http

The **Felis.Http** project contains the implementation of http endpoints for publish and subscribe to queues.

**Requirements**

- [.NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/overview)
- [JSON](https://docs.foursquare.com/analytics-products/docs/data-formats-json)
- [TLS 1.3](https://tls13.xargs.org)
- [HTTP/3](https://caniuse.com/http3)
- [QUIC](https://quicwg.org/)

**Dependencies**

- Felis
- Microsoft.AspNetCore.Authentication.Certificate 8.0.8

**Usage of http broker**

The **Felis** broker can be used as http queue application.

Code example:

```
    var port = args.FirstOrDefault(a => a.StartsWith("--port="))?.Split("=")[1] ?? "7110";
    var certificateName = args.FirstOrDefault(a => a.StartsWith("--certificate-name="))?.Split("=")[1] ?? "Output.pfx";
    var certificatePassword = args.FirstOrDefault(a => a.StartsWith("--certificate-password="))?.Split("=")[1] ?? "Password.1";

    var certificate = new X509Certificate2(certificateName, certificatePassword);
    
    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        })
        .AddFelisBroker().WithHttp(new X509Certificate2(certificateName, certificatePassword), int.Parse(port))
        .ConfigureServices((_, services) =>
        {
            services.AddHostedService(provider => new Subscriber(provider.GetRequiredService<ILogger<Subscriber>>(), certificate, $"https://localhost:{port}"));
            services.AddHostedService(provider => new Publisher(provider.GetRequiredService<ILogger<Publisher>>(), certificate, $"https://localhost:{port}"));
        });

    var host = builder.Build();

    await host.RunAsync(cts.Token);
```
The example above initialize the **Felis Broker** in a console application, with console logging provider and with Http communication protocol.
It also provides a **Subscriber** and a **Publisher** as hosted services.
All this flow is totally out-of-process.

The **WithHttp** extension takes **certificate**, **port** and **certificateForwardingHeader** as input parameters to use the broker with **[mTLS](https://www.cloudflare.com/it-it/learning/access-management/what-is-mutual-tls/)** authentication.
You can see all the implementation in the **Felis.Broker.Http.Console** project.

**Message entity**

The JSON below represent the **Message entity** coming from the broker.

```
{
    "id": "ac4625da-e922-4c2b-a7e7-aef21ece963c",
    "queue": "test",
    "payload": "{\"description\":\"Test\"}",
    "timestamp": 1724421633359,
    "expiration": 1724421644459
}
```

The **Message entity** is made of:

Property | Type   | Context                                                              |
--- |--------|----------------------------------------------------------------------|
id | guid   | the message unique id assigned by the broker.                        |
queue | string | the queue where the message has been published.                      |
payload | string | the actual content of the message published on the queue.            |
timestamp | number | the timestamp of the message when it was published.                  |
expiration | number | the message's expiration timestamp. It can be null.                  |

**Publish of a message to a queue with POST**

This endpoint is used to publish a message with whatever contract in the payload, by queue, to every listener subscribed to Felis.
This endpoint returns the unique identifier of the message published, assigned by the broker.

```
curl -X 'POST' \
  'https://localhost:7110/queue' \
  -H 'accept: application/json' \
  -H 'content-Type: application/json' \
  -H 'x-ttl: 10' \
  -d '{
        "description": "Test description"
}'
```

****Request body****
```
{
        "description": "Test description"
}
```

The string of the message to dispatch. The json above its just an example.

****Request headers****

Header | Value                                 | Context                                                                                            |
--- |---------------------------------------|----------------------------------------------------------------------------------------------------|
accept | application/json                      | The accept header.                                                                                 |
content-Type | application/json                      | The content type returned.                                                                         |
x-ttl | 10  (a number that is more than zero) | How many seconds a message can live. If not specified (or 0 value is used) the message is durable. |

***Response***

Status code | Type | Context |
--- | --- | --- |
202 | AcceptedResult | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |

**Subscribe to a queue with GET**

This endpoint is used to subscribe to a subset of queues using application/x-ndjson content-type to stream structured data.

```
curl -X 'GET' \
  'https://localhost:7110/queue' \
  -H 'accept: application/json' \
  -H 'x-exclusive: false' \
```

***Request route***

Property | Type | Context |
--- | --- | --- |
queue | string | the queue to subscribe to. |

****Request Headers****

Header | Value                                | Context                                                                     |
--- |--------------------------------------|-----------------------------------------------------------------------------|
accept | application/json                     | The accept header.                                                          |
x-exclusive | true/false                     | Tells to the broker that this subscriber is an exclusive one for the queue, so the messages will all go only to it.                                                          |

***Response***

Status code | Type | Context                                                                                                       |
--- | --- |---------------------------------------------------------------------------------------------------------------|
200 | Ok | When the subscription is successfully made and the application/x-ndjson header is returned to the client with the **Message entity**. |
204 | NoContentResult | When nothing is more available from the queue |
400 | BadRequestResult | When a validation or something not related to the authorization process fails.                                |
401 | UnauthorizedResult | When an operation fails due to missing authorization.                                                         |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context.                                             |

This endpoint pushes to the subscriber the **Message entity**.

****Response Headers****

Header | Value                | Context                                   |
--- |----------------------|-------------------------------------------|
content-Type | application/x-ndjson | The content type returned as json stream. |
cache-control | no-cache             | It avoids to cache the response           |
connection | keep-alive           | It keeps the connection alive             |

**How can I test it?**

This repository provides the examples of usage:

- **Felis.Broker.Http.Console**

**Felis.Broker.Http.Console**

A console application, that references **Felis.Http** project.
