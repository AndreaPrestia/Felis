# Felis
A light-weight web based message broker totally written in .NET based on HTTP3/QUIC.

The Felis project is made by the **Broker** part, containing the logic for dispatching, storing and validating messages.
It stores the message in a **LiteDB** storage.

**Requirements**

- .NET 8
- QUIC support
- HTTP3 support
- TLS13 support

**Dependencies**

- LiteDB 5.0.21
- Microsoft.AspNetCore.Authentication.Certificate 8.0.8
- Microsoft.Extensions.Logging.Abstractions 8.0.1

**Usage of Broker**

Code example:

```
    var currentDirectory = Path.GetDirectoryName(Directory.GetCurrentDirectory());
    var pfxPath = Path.Combine(currentDirectory!, @"..\..\..\Output.pfx");
    var certificatePath = Path.GetFullPath(pfxPath);

    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        })
        .AddFelisBroker(certificatePath, "Password.1", 7110);

    var host = builder.Build();

    await host.RunAsync();
```
The example above initialize the **Felis Broker** in a console application, using logging to console.

The **AddFelisBroker** method takes **certPath**, **certPassword**, **port** as input parameters to use the broker with mTLS authentication.

**Publish of a message to a topic with POST**

This endpoint is used to publish a message with whatever contract in the payload, by topic, to every listener subscribed to Felis.
This endpoint returns the unique identifier of the message published, assigned by the broker.

```
curl -X 'POST' \
  'https://localhost:7110/topic' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
        "description": "Test description"
}'
```

***Request***

The string of the message to dispatch.

***Response***

Status code | Type | Context |
--- | --- | --- |
202 | AcceptedResult | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |

**Subscribe to a topic with GET**

This endpoint is used to subscribe to a subset of topics using SSE. It is **not** documented in swagger.

```
curl -X 'GET' \
  'https://localhost:7110/topic' \
  -H 'accept: application/json'
```

***Request***

Property | Type | Context |
--- | --- | --- |
topic | string | the topic to subscribe to. |

***Response***

Status code | Type | Context |
--- | --- | --- |
200 | Ok | When the SSE subscription is successfully made and the text/event-stream header is returned to the client. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |

This endpoint pushes to the subscriber this json:

```
{
        "id": "ac4625da-e922-4c2b-a7e7-aef21ece963c",
        "topic": "test",
        "payload": "{\"description\":\"Test\"}",
        "timestamp": 1724421633359
}
```
The JSON above represent the **Message** coming from the broker.

The **Message** entity is made of:

Property | Type   | Context                                                   |
--- |--------|-----------------------------------------------------------|
id | guid   | the message unique id assigned by the broker.             |
topic | string | the topic where the message has been published.           |
payload | string | the actual content of the message published on the topic. |
timestamp | number | the timestamp of the message when it was published.       |

**How can I test it?**

This repository provides the examples of usage:

- **Felis.Broker.Console**
- **Felis.Publisher.Net.Console**
- **Felis.Subscriber.Net.Console**
- **Felis.Publisher.Node.Console**
- **Felis.Subscriber.Node.Console**
- **Felis.Publisher.Python.Console**
- **Felis.Subscriber.Python.Console**

**Felis.Broker.Console**

A console application, containing the two endpoints, **POST** and **GET** exposed by Felis Broker and documented in the following chapters.

**Publish a message**

To ease the testing process, I have two console application that publish to Felis broker.

This applications sends messages on the three topics of the **subscribers** examples:
- Test
- TestAsync
- TestError

**Usage of Publishers**

Just launch the **Publisher** applications in the **Examples** directory.

**Subscribe to a topic**

To ease the testing process, I have two console applications that subscribe to Felis broker.

These application contains the logic to subscribe to messages by topic.

**Usage of Subscribers**

Just launch the **Subscriber** applications in the **Examples** directory.

**Conclusion**

Though there is room for further improvement, the project is fit for becoming a sound and usable product in a short time. I hope that my work can inspire similar projects or help someone else.

**TODO**

- Unit testing.
- Stress testing.
