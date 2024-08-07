# Felis
A light-weight web based message broker totally written in .NET.

![FelisDiagram.png](Images%2FFelisDiagram.png)

The Felis project is made by the **Broker** part, containing the logic for dispatching, storing and validating messages. It stores the message in a LiteDB storage.

**How can I use it?**

This repository provides the examples of usage:

- **Felis.Broker.Console**
- **Felis.Publisher.Net.Console**
- **Felis.Subscriber.Net.Console**
- **Felis.Publisher.Node.Console**
- **Felis.Subscriber.Node.Console**

**Felis.Broker.Console**

An console application, containing the three endpoints exposed by Felis Broker.

The three endpoints are:

- publish POST
- subscribe GET
- subscribers/{topic} GET

These endpoints are documented in the following page:

```
https://localhost:7110/swagger/index.html
```

**publish**

This endpoint is used to publish a message with whatever contract in the payload, by topic, to every listener subscribed to Felis.
This endpoint returns the unique identifier of the message published, assigned by the broker.

```
curl -X 'POST' \
  'https://localhost:7110/publish' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
        "topic": "test",
        "payload": "{\"description\":\"Test\"}"
}'
```

***Request***
Property | Type | Context |
--- | --- | --- |
id | guid | the message global unique identifier. |
topic | string | the actual content of the topic of the message to dispatch. |
payload | string | Json string of the message to dispatch. |

***Response***
Status code | Type | Context |
--- | --- | --- |
202 | AcceptedResult | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |

**subscribe**

This endpoint is used to subscribe to a subset of topics using SSE. It is **not** documented in swagger.

```
curl -X 'GET' \
  'https://localhost:7110/subscribe?topics=topic1,topic2' \
  -H 'accept: application/json'
```

***Request***
Property | Type | Context |
--- | --- | --- |
topics | string | the list of comma separated topics to subscribe to. |

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
        "payload": "{\"description\":\"Test\"}"
}
```
The JSON above represent the **Message** coming from the broker.

The **Message** entity is made of:

Property | Type | Context |
--- | --- | --- |
id | guid | the message unique id assigned by the broker. |
topic | string | the topic where the message has been published. |
payload | string | the actual content of the message published on the topic. |

**subscribers/{topic}**

This endpoint provides a list of the subscribers connected to Felis that consume a specific topic provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/subscribers/topic' \
  -H 'accept: application/json'
```

***Response***

```
[
   {
      "ipAddress":"192.168.1.1",
      "hostname":"host",
      "topics":[
            "topic"
      ]
   }
]
```
This endpoint returns an array of clients.

Property | Type          | Context                                                |
--- |---------------|--------------------------------------------------------|
ipAddress | string        | The ipAddress property of the subscriber.              |
hostname | string        | The hostname property of the subscriber.                 |
topics | array<string> | This property contains the array of topics subscribed. |

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
        .AddFelisBroker("username", "password", 7110);

    var host = builder.Build();

    await host.RunAsync();
```
The example above initialize the **Felis Broker** in a console application.

The **AddFelisBroker** method takes **username**, **password**, **port** as input parameters to use the broker with basic authentication.

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

- Code refactoring.
- Unit testing.
- Stress testing.
- Auth flow. (NO BASIC AUTH ANYMORE)
