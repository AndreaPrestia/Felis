# Felis
A light-weight message broker totally written in .NET based on HTTP3/QUIC and JSON.

The Felis project contains the logic for dispatching, storing and validating messages.
It stores the messages in a **LiteDB** database.

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
The example above initialize the **Felis Broker** in a console application, with console logging provider.

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

When an error occurs it is used the standard [RFC7807](https://datatracker.ietf.org/doc/html/rfc7807) to return HTTPS APIs errors with a **Content-Type** header with value **application/problem+json** 
and the following object:

```
{
    Type = "https://httpstatuses.io/500",
    Detail = "Error details",
    Status = 500,
    Title = "An error has occurred",
    Instance = "/Test POST",
}
```

**Subscribe to a topic with GET**

This endpoint is used to subscribe to a subset of topics using application/x-ndjson content-type to stream structured data. 

```
curl -X 'GET' \
  'https://localhost:7110/topic' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/x-ndjson'
```

***Request***

Property | Type | Context |
--- | --- | --- |
topic | string | the topic to subscribe to. |

***Response***

Status code | Type | Context                                                                                                       |
--- | --- |---------------------------------------------------------------------------------------------------------------|
200 | Ok | When the subscription is successfully made and the application/octet-stream header is returned to the client. |
204 | NoContentResult | When nothing is more available from the topic |
400 | BadRequestResult | When a validation or something not related to the authorization process fails.                                |
401 | UnauthorizedResult | When an operation fails due to missing authorization.                                                         |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context.                                             |

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

When an error occurs it is used the standard [RFC7807](https://datatracker.ietf.org/doc/html/rfc7807) to return HTTPS APIs errors with a **Content-Type** header with value **application/problem+json**
and the following object:

```
{
    Type = "https://httpstatuses.io/500",
    Detail = "Error details",
    Status = 500,
    Title = "An error has occurred",
    Instance = "/Test GET",
}
```

**How can I test it?**

This repository provides the examples of usage:

- **Felis.Broker.Console**
- **Felis.Publisher.Net.Console**
- **Felis.Subscriber.Net.Console**
- **Felis.Publisher.Node.Console**
- **Felis.Subscriber.Node.Console**
- **Felis.Publisher.Python.Console**
- **Felis.Subscriber.Python.Console**

Please be aware that the Python examples could have some problem sometimes, i'm not an expert of that language :P.

**Felis.Broker.Console**

A console application, containing the two endpoints, **POST** and **GET** exposed by Felis Broker and documented in the following chapters.

**Publish a message**

To ease the testing process, I have two console application that publish to Felis broker.

These applications sends messages on the **Test** topic of the **subscribers** examples.

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
- Dockerized examples.
