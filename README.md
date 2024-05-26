# Felis
A message broker totally written in .NET, based on SignalR.

![FelisDiagram.jpg](Images%2FFelisDiagram.jpg)

The Felis project is composed of two parts:

- **Router**, containing the logic for dispatching, storing and validating messages.
- **Client**, containing the logic of the client, that will consume a message by topic, using a specific entity contract.

**How can I use it?**

This repository provides two examples of usage:

- **Felis.Client.Test**
- **Felis.Router.Test**

**Felis.Router.Test**

An ASP-NET minimal API application, containing the ten endpoints exposed by Felis.

The ten endpoints are:

- messages/{topic}/dispatch
- messages/{id}/consume
- messages/{id}/process
- messages/{id}/error
- messages/{topic}/ready/purge
- messages/{topic}/consumers
- messages/{topic}/ready
- messages/{topic}/sent
- messages/{topic}/error
- messages/{topic}/consumed
- consumers/{connectionId}/messages
- consumers/{connectionId}/messages/{topic}

These endpoints are documented in the following page:

```
https://localhost:7103/swagger/index.html
```

**messages/{topic}/dispatch**

This endpoint is used to dispatch a message with whatever contract in the payload, by topic, to every listener connected to Felis.

```
curl -X 'POST' \
  'https://localhost:7110/messages/topic/dispatch' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
    "header": {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "topic": "test"
    },
    "content": {
        "payload": "{\"description\":\"Test\"}"
    }
}'
```

***Request***
Property | Type | Context |
--- | --- | --- |
header | object | the message header, containing the metadata of the message. |
header.id | guid | the message global unique identifier. |
header.topic | string | the content of the topic of the message to dispatch. |
content | object | the message content. |
content.payload | string | Json string of the message to dispatch. |

***Response***
Status code | Type | Context |
--- | --- | --- |
201 | CreatedResult object | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |
409 | Conflict | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

**messages/{id}/consume**

This endpoint informs Felis when a client successfully consumes a message. It is used to keep track of the operations (ACK).

```
curl -X 'POST' \
  'https://localhost:7110/messages/3fa85f64-5717-4562-b3fc-2c963f66afa6/consume' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "connectionId": "AYhRMfzMA62BvJn3paMczQ",
    "timestamp": 1716564304386
}'
```

***Request***
Property | Type | Context |
--- | --- | --- |
id | guid | the message global unique identifier. |
connectionId | string | the actual value of the connectionId of the message that throws an error. |
timestamp | long | the timestamp from client. |

***Response***
Status code | Type | Context |
--- | --- | --- |
204 | NoContentResult | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |
409 | Conflict | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

**messages/{id}/process**

This endpoint informs Felis when a client successfully processed a message. 

```
curl -X 'POST' \
  'https://localhost:7110/messages/3fa85f64-5717-4562-b3fc-2c963f66afa6/process' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "connectionId": "AYhRMfzMA62BvJn3paMczQ",
    "timestamp": 1716564304386
}'
```

***Request***
Property | Type | Context |
--- | --- | --- |
id | guid | the message global unique identifier. |
connectionId | string | the actual value of the connectionId of the message that throws an error. |
timestamp | long | the timestamp from client. |

***Response***
Status code | Type | Context |
--- | --- | --- |
204 | NoContentResult | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |
409 | Conflict | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

**messages/{id}/error**

This endpoint informs Felis when a client encounters errors while consuming a message. It is used to keep track of the operations (ACK).
If no retry policy is provided the message is not requeued but only logged in Felis.

```
curl -X 'POST' \
  'https://localhost:7110/messages/3fa85f64-5717-4562-b3fc-2c963f66afa6/error' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "connectionId": "AYhRMfzMA62BvJn3paMczQ",
    "error": {
        "title": "string",
        "detail": "string"
    },
    "retryPolicy": {
        "attempts": 3
    }
}'
```

***Request***
Property | Type | Context |
--- | --- | --- |
id | guid | the message global unique identifier. |
connectionId | string | the actual value of the connectionId of the message that throws an error. |
error | object | The object containing the error occurred. |
error.title | string | The .NET exception message. |
error.detail | string | The .NET exception stacktrace. |
retryPolicy | object | The object containing the retry policy to apply to the message. |
retryPolicy.attempts | int | The number of attempts to retry to send a message. |

***Response***
Status code | Type | Context |
--- | --- | --- |
204 | NoContentResult | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |
409 | Conflict | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

**messages/{topic}/ready/purge**

This endpoint tells the router to purge the ready queue for a specific topic. It is irreversible.

```
curl -X 'DELETE' \
  'https://localhost:7110/messages/topic/purge' \
  -H 'accept: application/json'
  ```

***Request***

Property | Type | Context |
--- | --- | --- |
topic | string | the actual value of the topic of the message queue to purge. |

***Response***

Status code | Type | Context |
--- | --- | --- |
204 | NoContentRequest | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |
409 | Conflict | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

**messages/{topic}/consumers**

This endpoint provides a list of the clients connected to Felis that consume a specific topic provided in the route. It exposes only the clients that are configured with the property **IsPublic** to **true**, which makes the clients discoverable.

```
curl -X 'GET' \
  'https://localhost:7110/messages/topic/consumers' \
  -H 'accept: application/json'
```

***Response***

Status code | Type                  | Context |
--- |-----------------------| --- |
200 | Array<Consumer>       | When the request is successfully processed. |
400 | BadRequestResult      | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult    | When an operation fails due to missing authorization. |
403 | ForbiddenResult       | When an operation fails because it is not allowed in the context. |
409 | Conflict              | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

****Response content****

```
[
   {
      "ipAddress":"192.168.1.1",
      "hostname":"host",
      "topics":["topic"],
      "unique": false
   }
]
```
This endpoint returns an array of clients.

Property | Type          | Context                                                                                                     |
--- |---------------|-------------------------------------------------------------------------------------------------------------|
ipAddress | string        | The ipAddress property of the consumer.                                                                     |
hostname | string        | The hostname property of the consumer.                                                                      |
topics | array<string> | This property contains the array of topics subscribed by the consumer.                                      |
unique | boolean       | This property tells the router whether the consumer is configured to be unique.                             |

**message/{topic}/ready**

This endpoint provides a list of the message ready to sent in Felis for a specific topic provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/topic/ready' \
  -H 'accept: application/json'
```

***Response***

Status code | Type                  | Context |
--- |-----------------------| --- |
200 | Array<Message>        | When the request is successfully processed. |
400 | BadRequestResult      | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult    | When an operation fails due to missing authorization. |
403 | ForbiddenResult       | When an operation fails because it is not allowed in the context. |
409 | Conflict              | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

****Response content****

```
[
  {
    "header": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "topic": "string",
      "timestamp": 0
    },
    "content": {
      "payload": "string"
    }
  }
]
```
This endpoint returns an array of message.

Property | Type | Context |
--- | --- | --- |
header | object | the message header, containing the metadata of the message. |
header.id | guid | the message global unique identifier. |
header.topic | string | the actual value of the topic of the message ready to be sent. |
content | object | the message content. |
content.payload | string | Json string of the message ready to be sent. |

**message/{topic}/sent**

This endpoint provides a list of the message sent in Felis for a specific topic provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/topic/sent' \
  -H 'accept: application/json'
```

***Response***

Status code | Type                  | Context |
--- |-----------------------| --- |
200 | Array<Message>        | When the request is successfully processed. |
400 | BadRequestResult      | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult    | When an operation fails due to missing authorization. |
403 | ForbiddenResult       | When an operation fails because it is not allowed in the context. |
409 | Conflict              | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

****Response content****

```
[
  {
    "header": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "topic": "string",
      "timestamp": 0
    },
    "content": {
      "payload": "string"
    }
  }
]
```
This endpoint returns an array of message.

Property | Type | Context |
--- | --- | --- |
header | object | the message header, containing the metadata of the message. |
header.id | guid | the message global unique identifier. |
header.topic | string | the actual value of the topic of the message ready to be sent. |
content | object | the message content. |
content.payload | string | Json string of the message ready to be sent. |

**messages/{topic}/error**

This endpoint provides a list of the message that are gone in the error queue for a specific topic provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/topic/error' \
  -H 'accept: application/json'
```

***Response***

Status code | Type                  | Context |
--- |-----------------------| --- |
200 | Array<ErrorMessage>   | When the request is successfully processed. |
400 | BadRequestResult      | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult    | When an operation fails due to missing authorization. |
403 | ForbiddenResult       | When an operation fails because it is not allowed in the context. |
409 | Conflict              | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

****Response content****

```
[
    {
        "message": {
            "content": {
                "json": "string"
            },
            "header": {
                "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                "timestamp": 0,
                "topic": "string"
            }
        },
        "errors": [
            "connectionId": "AYhRMfzMA62BvJn3paMczQ",
            "details": [
                {
                    "title": "string",
                    "detail": "string"
                }
            ]
            "retryPolicy": {
                "attempts": 1
            }
        ]
    }
]
```
This endpoint returns an array of messages with related error and connection id where the error happened.

Property | Type         | Context                                                                   |
--- |--------------|---------------------------------------------------------------------------|
message | object       | The message entity used by Felis system.                                  |
message.header | object       | the message header, containing the metadata of the message.               |
message.header.id | guid         | the message global unique identifier.                                     |
message.header.topic | string       | the actual value of the topic of the message that throws an error.        |
message.content | object       | the message content.                                                      |
message.content.payload | string       | Json string of the message that throws an error.                          |
errors | array<object | array of the errors occurred on the message                               |
errors.connectionId | string       | the actual value of the connectionId of the message that throws an error. |
errors.details | array<object> | The array of object containing the error details occurred.                |
errors.details.title | string       | The .NET exception message.                                               |
errors.details.detail | string       | The .NET exception stacktrace.                                            |

**messages/{topic}/consumed**

This endpoint provides a list of the message that are been consumed for a specific topic provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/topic/consumed' \
  -H 'accept: application/json'
```

***Response***

Status code | Type                   | Context |
--- |------------------------| --- |
200 | Array<ConsumedMessage> | When the request is successfully processed. |
400 | BadRequestResult       | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult     | When an operation fails due to missing authorization. |
403 | ForbiddenResult        | When an operation fails because it is not allowed in the context. |
409 | Conflict               | When an operation fails because it conflicts with another one. |
500 | Internal server error  | When an operation fails for another reason. |

****Response content****

```
[
    {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "connectionId": "AYhRMfzMA62BvJn3paMczQ",
        "timestamp": 0
    }
]
```
This endpoint returns an array of messages that are consumed, with related connection id and timestamp.

Property | Type | Context                                                                         |
--- | --- |---------------------------------------------------------------------------------|
id | guid | the message global unique identifier.                                           |
connectionId | string | the actual value of the connectionId of the message consumed. |
timestamp | long | The unix time in milliseconds that provides the consume time.                   |

**consumers/{connectionId}/messages**

This endpoint provides a list of the message that are been consumed for the connection id provided in route.

```
curl -X 'GET' \
  'https://localhost:7110/consumers/AYhRMfzMA62BvJn3paMczQ/messages' \
  -H 'accept: application/json'
```

***Response***

Status code | Type                  | Context |
--- |-----------------------| --- |
200 | Array<Message>        | When the request is successfully processed. |
400 | BadRequestResult      | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult    | When an operation fails due to missing authorization. |
403 | ForbiddenResult       | When an operation fails because it is not allowed in the context. |
409 | Conflict              | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

****Response content****

```
[
    {
        "message": {
            "content": {
                "json": "string"
            },
            "header": {
                "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                "timestamp": 0,
                "topic": "string"
            }
        },
        "connectionId": "AYhRMfzMA62BvJn3paMczQ",
	    "timestamp": 0
    }
]
```
This endpoint returns an array of messages that are consumed, with related connection id and timestamp.

Property | Type | Context |
--- | --- | --- |
message | object | The message entity used by Felis system. |
message.header | object | the message header, containing the metadata of the message. |
message.header.id | guid | the message global unique identifier. |
message.header.topic | string | the actual value of the topic of the message consumed. |
message.content | object | the message content. |
message.content.payload | string | Json string of the message consumed. |
connectionId | string | the actual value of the connectionId of the message consumed. |
timestamp | long | The unix time in milliseconds that provides the consume time. |

**consumers/{connectionId}/messages/{topic}**

This endpoint provides a list of the message that are been consumed for the connection id provided in route and for a specific topic.

```
curl -X 'GET' \
  'https://localhost:7110/consumers/AYhRMfzMA62BvJn3paMczQ/messages/topic' \
  -H 'accept: application/json'
  ```

***Response***

Status code | Type                  | Context |
--- |-----------------------| --- |
200 | Array<Message>        | When the request is successfully processed. |
400 | BadRequestResult      | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult    | When an operation fails due to missing authorization. |
403 | ForbiddenResult       | When an operation fails because it is not allowed in the context. |
409 | Conflict              | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

****Response content****

```
[
    {
       "message": {
            "content": {
                "json": "string"
            },
            "header": {
                "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                "timestamp": 0,
                "topic": "string"
            }
        },
        "connectionId": "AYhRMfzMA62BvJn3paMczQ",
	"timestamp": 0
    }
]
```
This endpoint returns an array of messages that are consumed, with related connection id and timestamp.

Property | Type | Context |
--- | --- | --- |
message | object | The message entity used by Felis system. |
message.id | guid | the message global unique identifier. |
message.header | object | the message header, containing the metadata of the message. |
message.header.topic | string | the actual value of the topic of the message consumed. |
message.content | object | the message content. |
message.content.json | string | Json string of the message consumed. |
connectionId | string | the actual value of the connectionId of the message consumed. |
timestamp | long | The unix time in milliseconds that provides the consume time. |

**Program.cs**

Add these two lines of code:

```
builder.AddFelisRouter(); => this line adds the FelisRouter with its related implementation for clients and dispatchers
app.UseFelisRouter(); => this line uses the implementations and endpoints
```

**Felis.Client.Test**

To ease the testing process, I have implemented an ASP-NET minimal API application that exposes a publish endpoint.

This application contains a class, called TestConsumer, that implements the Consume<T> abstract class. It contains the Process(T entity) method implemented. It only shows how messages are intercepted.

**Program.cs**

Just add the following line of code:
```
builder.AddFelisClient("https://localhost:7103", false 15, 5);
```

The signature of **AddFelisClient** method is made of:

Parameter | Type    | Context                                                                                                                                                                                                                                          |
--- |---------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
routerEndpoint | string  | The FelisRouter endpoint that the client must subscribe.                                                                                                                                                                                         |
unique | boolean | Tells to the FelisRouter if the consumer is unique.                                                                                                                                                                                              |
pooledConnectionLifetimeMinutes | int     | The internal http client PooledConnectionLifetimeMinutes. Not mandatory. Default value is 15.                                                                                                                                                    |
maxAttempts | int     | It tells the router the maximum number of attempts that should be made to resend a message in the error queue, according to the retry policy that you want to apply. All the attempts are logged in the router. Not mandatory, default value is 0. |

**How do I use a consumer?**

It is very simple. Just create a class that implements the IConsume<T> interface.
See two examples from GitHub here below:

***Sync mode***
```
using Felis.Client.Test.Models;
using System.Text.Json;

namespace Felis.Client.Test;

[Topic("Test")]
public class TestConsumer : IConsume<TestModel>
{
	public async void Process(TestModel entity)
	{
		Console.WriteLine("Sync mode");
		Console.WriteLine(JsonSerializer.Serialize(entity));
	}
}
```

***Async mode***
```
using System.Text.Json;
using Felis.Client.Test.Models;

namespace Felis.Client.Test;

[Topic("TestAsync")]
public class TestConsumerAsync : IConsume<TestModel>
{
    public async void Process(TestModel entity)
    {
        Console.WriteLine("Async mode");
        await Task.Run(() =>
            Console.WriteLine(JsonSerializer.Serialize(entity)));
    }
}
```

**Conclusion**

Though there is room for further improvement, the project is fit for becoming a sound and usable product in a short time. I hope that my work can inspire similar projects or help someone else.

**TODO**

- Implement an authorization mechanism to make Felis available in public networks.
- Code refactoring.
- Unit testing.
- Stress testing.
- Clusterization design.

  

