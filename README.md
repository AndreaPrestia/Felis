# ![Alt text](Felis.jpg)

A light-weight message broker totally written in .NET based on HTTP3, QUIC and JSON.

The **Felis** project contains the logic for dispatching, storing and validating messages.
It stores the messages in a **LiteDB** database.
It behaves as message queue and broadcaster if a specific message for a topic is tagged with broadcast property.

**Requirements**

- [.NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/overview)
- [TLS 1.3](https://tls13.xargs.org)
- [HTTP/3](https://caniuse.com/http3)
- [QUIC](https://quicwg.org/)
- [JSON](https://docs.foursquare.com/analytics-products/docs/data-formats-json)

**Dependencies**

- LiteDB 5.0.21
- Microsoft.AspNetCore.Authentication.Certificate 8.0.8

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
    .AddFelisBroker("Output.pfx", "Password.1", 7110, "Felis.db");

var host = builder.Build();

await host.RunAsync();
```
The example above initialize the **Felis Broker** in a console application, with console logging provider.

The **AddFelisBroker** method takes **certPath**, **certPassword**, **port** and **databasePath** as input parameters to use the broker with [mTLS](https://www.cloudflare.com/it-it/learning/access-management/what-is-mutual-tls/) authentication.

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

**Publish of a message to a topic with POST**

This endpoint is used to publish a message with whatever contract in the payload, by topic, to every listener subscribed to Felis.
This endpoint returns the unique identifier of the message published, assigned by the broker.

```
curl -X 'POST' \
  'https://localhost:7110/topic' \
  -H 'accept: application/json' \
  -H 'content-Type: application/json' \
  -H 'x-broadcast: false' \
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
x-broadcast | true/false   | Tells if the message must be broadcasted to all subscribers. If not provided or set to false Felis broker will send enqueued message only to one subscriber in a load balanced manner.                        |
x-ttl | 10  (a number that is more than zero) | How many seconds a message can live. If not specified (or 0 value is used) the message is durable. |

***Response***

Status code | Type | Context |
--- | --- | --- |
202 | AcceptedResult | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |

**Subscribe to a topic with GET**

This endpoint is used to subscribe to a subset of topics using application/x-ndjson content-type to stream structured data. 

```
curl -X 'GET' \
  'https://localhost:7110/topic' \
  -H 'accept: application/json' \
  -H 'x-exclusive: false' \
```

***Request route***

Property | Type | Context |
--- | --- | --- |
topic | string | the topic to subscribe to. |

****Request Headers****

Header | Value                                | Context                                                                     |
--- |--------------------------------------|-----------------------------------------------------------------------------|
accept | application/json                     | The accept header.                                                          |
x-exclusive | true/false                     | Tells to the broker that this subscriber is an exclusive one for the topic, so the messages will all go only to it.                                                          |

***Response***

Status code | Type | Context                                                                                                       |
--- | --- |---------------------------------------------------------------------------------------------------------------|
200 | Ok | When the subscription is successfully made and the application/x-ndjson header is returned to the client with the **Message entity**. |
204 | NoContentResult | When nothing is more available from the topic |
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

**Reset a topic with DELETE**

This endpoint is used to publish a message with whatever contract in the payload, by topic, to every listener subscribed to Felis.
This endpoint returns the unique identifier of the message published, assigned by the broker.

```
curl -X 'DELETE' \
  'https://localhost:7110/topic' \
```

***Response***

Status code | Type               | Context |
--- |--------------------| --- |
200 | SuccessResult      | When the request is successfully processed. |
400 | BadRequestResult   | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult    | When an operation fails because it is not allowed in the context. |

This endpoint returns the number of messages deleted from the topic. It deletes all the messages waiting to be dispatched to subscribers.

**Get messages from a topic with GET**

This endpoint is used to subscribe to a subset of topics using application/x-ndjson content-type to stream structured data. 

```
curl -X 'GET' \
  'https://localhost:7110/topic/1/10' \
  -H 'accept: application/json' \
```

***Request route***

Property | Type | Context |
--- | --- | --- |
topic | string | the topic to subscribe to. |
page | number | the page number to search. |
size | number | the size of the request. The maximum allowed is 100 |

****Request Headers****

Header | Value                                | Context                                                                     |
--- |--------------------------------------|-----------------------------------------------------------------------------|
accept | application/json                     | The accept header.                                                          |

***Response***

Status code | Type | Context                                                                                                       |
--- | --- |---------------------------------------------------------------------------------------------------------------|
200 | Ok | When the operation is successfully fullfilled. |
204 | NoContentResult | When nothing is more available from the topic |
400 | BadRequestResult | When a validation or something not related to the authorization process fails.                                |
401 | UnauthorizedResult | When an operation fails due to missing authorization.                                                         |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context.                                             |

This endpoint return an array of **Message entity**.

****Response Headers****

Header | Value                | Context                                   |
--- |----------------------|-------------------------------------------|
content-Type | application/json | The content type returned as json. |


**Error responses from endpoints**

When an error occurs during an API request it is used the standard [RFC7807](https://datatracker.ietf.org/doc/html/rfc7807) to return HTTPS APIs errors with a **Content-Type** header with value **application/problem+json**
and the following object:

```
{
    Type = "https://httpstatuses.io/500",
    Detail = "Error details",
    Status = 500,
    Title = "An error has occurred",
    Instance = "/Test/1/10 GET",
}
```

**How can I test it?**

This repository provides the examples of usage:

- **Felis.Broker.Console**
- **Felis.Publisher.Node.Console**
- **Felis.Subscriber.Node.Console**

**Felis.Broker.Console**

A console application, that reference Felis project.

**Publish a message**

To ease the testing process, I have a NodeJS console applications that publish to Felis broker with multiple topics.

This applications sends messages on the **Generic**, **TTL**, **Broadcast** and **Exclusive** topics.

**Usage of Publishers**

Just launch the **Publisher** applications in the **Examples** directory.

**Subscribe to a topic**

To ease the testing process, I have a NodeJS console applications that subscribe to Felis broker with multiple subscribers.

This application contains the logic to subscribe to messages by topic.

**Usage of Subscribers**

Just launch the **Subscriber** applications in the **Examples** directory.

**Conclusion**

Though there is room for further improvement, the project is fit for becoming a sound and usable product in a short time. I hope that my work can inspire similar projects or help someone else.
I want to add some sort of alerting when something strange occurs, for example when a queue is too big or too much fails occur.
