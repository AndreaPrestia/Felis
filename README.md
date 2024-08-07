# Felis
A light-weight message broker totally written in .NET.

![FelisDiagram.jpg](Images%2FFelisDiagram.jpg.png)

The Felis project is composed of three parts:

- **Broker**, containing the logic for dispatching, storing and validating messages. It stores the message in a LiteDB storage.
- **Publisher**, containing the logic of the publisher, that will publish a message by topic, using a specific entity contract.
- **Subscriber**, containing the logic of the subscriber, that will consume a message by topic, using a specific entity contract.

**How can I use it?**

This repository provides the examples of usage:

- **Felis.Broker.Console**
- **Felis.Publisher.Console**
- **Felis.Subscriber.Console**

**Felis.Broker.Console**

An console application, containing the three endpoints exposed by Felis Broker.

The three endpoints are:

- publish
- subscribe
- subscribers/{topic}

These endpoints are documented in the following page:

```
https://localhost:7110/swagger/index.html
```

**publish**

This endpoint is used to publish a message with whatever contract in the payload, by topic, to every listener subscribed to Felis.

```
curl -X 'POST' \
  'https://localhost:7110/publish' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "topic": "test"
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

This endpoint is used to subscribe to a subset of topics using SSE.

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

**Usage of Felis Broker**

Code example:

```
    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        })
        .AddGerryBroker("username", "password", 7110);

    var host = builder.Build();

    await host.RunAsync();
```
The example above initialize the **Felis Broker** in a console application.

The **AddGerryBroker** method takes **username**, **password**, **port** as input parameters to use the broker with basic authentication.

**Felis.Publisher.Console**

To ease the testing process, I have a console application that publishes to a Felis broker.

This application sends messages on the three topics of the **subscriber** example:
- Test
- TestAsync
- TestError

**Usage of Felis Publisher**

Just see the following lines of code:
```
var hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.AddGerryPublisher("https://username:password@localhost:7110");

var host = hostBuilder.Build();

var messagePublisher = host.Services.GetRequiredService<MessagePublisher>();

while (true)
{
    await messagePublisher.PublishAsync("Test", new TestModel()
    {
        Description = "Test"
    }, CancellationToken.None);
    
    await Task.Delay(5000);
    
    await messagePublisher.PublishAsync("TestAsync", new TestModel()
    {
        Description = "TestAsync"
    }, CancellationToken.None);
    
    await Task.Delay(1000);
    
    await messagePublisher.PublishAsync("TestError", new TestModel()
    {
        Description = "TestError"
    }, CancellationToken.None);
}
```

These code registers a **Felis Publisher** and publishes a message for every topic that we have on the subscriber example, using a Task.Delay to simulate an async work.

The signature of **AddGerryPublisher** method is made of:

Parameter | Type | Context                                                                                       |
--- | --- |-----------------------------------------------------------------------------------------------|
connectionString | string | The Felis broker connection string that the client must subscribe with related credentials.   |
pooledConnectionLifetimeMinutes | int | The internal http client PooledConnectionLifetimeMinutes. Not mandatory. Default value is 15. |

***Publish to a Topic***

To use a **publisher** you have to use the **MessagePublisher** class.

Here an example:
```
var messagePublisher = host.Services.GetRequiredService<MessagePublisher>();

await messagePublisher.PublishAsync("Test", new TestModel()
    {
        Description = "Test"
    }, CancellationToken.None);
```

**Felis.Subscriber.Console**

To ease the testing process, I have a console application that subscribes to a Felis broker.

This application contains three classes, called TestSubscriber, TestSubscriberAsync and TestSubscriberWithError, that implement the ISubscribe<T> interface. They contain the Process(T entity) method implemented. They only show how messages are intercepted.

**Usage of Felis Subscriber**

Just see the following lines of code:
```
var hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.AddGerrySubscriber("https://username:password@localhost:7110");

var host = hostBuilder.Build();

var messageSubscriber = host.Services.GetRequiredService<MessageSubscriber>();
await messageSubscriber.ConnectAsync(CancellationToken.None);
```
The example above registers a **Felis Subscriber** in a console application, retrieves the **MessageSubscriber** class and starts to listen for messages available on the **Felis Broker** for the topics declare in the **Subscriber** application.

The signature of **AddGerrySubscriber** method is made of:

Parameter | Type | Context                                                                                       |
--- | --- |-----------------------------------------------------------------------------------------------|
connectionString | string | The Felis broker connection string that the client must subscribe with related credentials.   |
pooledConnectionLifetimeMinutes | int | The internal http client PooledConnectionLifetimeMinutes. Not mandatory. Default value is 15. |

**How do I use a subscriber?**

To use a **subscriber** you have to use the **MessageSubscriber** class.

Here an example:
```
var messageSubscriber = host.Services.GetRequiredService<MessageSubscriber>();
await messageSubscriber.ConnectAsync(CancellationToken.None);
```

***Subscribe to a Topic***

It is very simple. Just create a class that implements the ISubscribe<T> interface.
See three examples from GitHub here below:

****Sync****
```
using Felis.Client.Test.Models;
using System.Text.Json;

namespace Felis.Client.Test;

[Topic("Test")]
public class TestSubscriber : ISubscribe<TestModel>
{
	public async void Process(TestModel entity)
	{
		Console.WriteLine("Sync mode");
		Console.WriteLine(JsonSerializer.Serialize(entity));
	}
}
```

****Async****

```
using System.Text.Json;
using Felis.Client.Test.Models;

namespace Felis.Client.Test;

[Topic("TestAsync")]
public class TestSubscriberAsync : ISubscribe<TestModel>
{
    public async void Process(TestModel entity)
    {
        Console.WriteLine("Async mode");
        await Task.Run(() =>
            Console.WriteLine(JsonSerializer.Serialize(entity)));
    }
}
```

****Throwing error****

```
using System.Text.Json;
using Felis.Client.Test.Models;

namespace Felis.Client.Test;

[Topic("TestError")]
public class TestSubscriberWithError : ISubscribe<TestModel>
{
    public void Process(TestModel entity)
    {
        throw new NotImplementedException("Example with exception");
    }
}
```

**Conclusion**

Though there is room for further improvement, the project is fit for becoming a sound and usable product in a short time. I hope that my work can inspire similar projects or help someone else.

**TODO**

- Code refactoring.
- Unit testing.
- Stress testing.
- Auth flow. (NO BASIC AUTH ANYMORE)