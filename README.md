# Felis
A message queue totally written in .NET, based on SignalR.

![FelisDiagram.jpg](Images%2FFelisDiagram.jpg)

The Felis project is composed of two parts:

- **Router**, containing the logic for dispatching, storing and validating messages.
- **Subscriber**, containing the logic of the subscriber, that will consume a message by topic, using a specific entity contract.

**How can I use it?**

This repository provides two examples of usage:

- **Felis.Subscriber.Test**
- **Felis.Router.Test**

**Felis.Router.Test**

An ASP-NET minimal API application, containing the ten endpoints exposed by Felis.

The ten endpoints are:

- messages/dispatch
- messages/consume
- messages/process
- messages/error
- messages/{topic}/ready/purge
- messages/{topic}/subscribers
- messages/{topic}/ready
- messages/{topic}/sent
- messages/{topic}/error
- messages/{topic}/consumed
- subscribers/{connectionId}/messages
- subscribers/{connectionId}/messages/{topic}

These endpoints are documented in the following page:

```
https://localhost:7103/swagger/index.html
```

**messages/dispatch**

This endpoint is used to dispatch a message with whatever contract in the payload, by topic, to every listener connected to Felis.

```
curl -X 'POST' \
  'https://localhost:7110/messages/dispatch' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
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

**messages/consume**

This endpoint informs Felis when a subscriber successfully consumes a message. It is used to keep track of the operations (ACK).

```
curl -X 'POST' \
  'https://localhost:7110/messages/consume' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
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
timestamp | long | the timestamp from subscriber. |

***Response***
Status code | Type | Context |
--- | --- | --- |
204 | NoContentResult | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |
409 | Conflict | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

**messages/process**

This endpoint informs Felis when a subscriber successfully processed a message. 

```
curl -X 'POST' \
  'https://localhost:7110/messages/process' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
  -d '{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "connectionId": "AYhRMfzMA62BvJn3paMczQ",
    "executionTimeMs": 1716564304386
}'
```

***Request***
Property | Type | Context |
--- | --- | --- |
id | guid | the message global unique identifier. |
connectionId | string | the actual value of the connectionId of the message that throws an error. |
executionTimeMs | long | the execution time in milliseconds of the message processing. |

***Response***
Status code | Type | Context |
--- | --- | --- |
204 | NoContentResult | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |
409 | Conflict | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

**messages/error**

This endpoint informs Felis when a subscriber encounters errors while consuming a message. It is used to keep track of the operations (ACK).
If no retry policy is provided in the topic attribute, by the subscriber, the message is not requeued but goes in the dead letter storage.

```
curl -X 'POST' \
  'https://localhost:7110/messages/error' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
  -d '{
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "connectionId": "AYhRMfzMA62BvJn3paMczQ",
    "error": {
        "title": "string",
        "detail": "string"
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
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
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

**messages/{topic}/subscribers**

This endpoint provides a list of the subscribers connected to Felis that consume a specific topic provided in the route. It exposes only the subscribers that are configured with the property **IsPublic** to **true**, which makes the subscribers discoverable.

```
curl -X 'GET' \
  'https://localhost:7110/messages/topic/subscribers' \
  -H 'accept: application/json'
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
```

***Response***

Status code | Type                  | Context |
--- |-----------------------| --- |
200 | Array<Subscriber>     | When the request is successfully processed. |
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
      "topics":[
      {
          "name": "topic",
          "unique": false,
          "retryPolicy": {
               "attempts": 2
          }
      }]
   }
]
```
This endpoint returns an array of subscribers.

Property | Type         | Context                                                                                              |
--- |--------------|------------------------------------------------------------------------------------------------------|
ipAddress | string       | The ipAddress property of the subscriber.                                                            |
hostname | string       | The hostname property of the subscriber.                                                             |
topics | array<Topic> | This property contains the array of topics subscribed by the subscriber.                             |
topics.name | string       | This property contains the topic name subscribed by the subscriber                                   |
topics.unique | boolean      | This property tells the router whether the topic has a unique consumer registered in the subscriber. |
topics.retryPolicy | object       | This property tells the router whether the topic has a retry policy to apply for the subscriber.     |
topics.retryPolicy.attempts | int          | This property tells the router whether the topic has the retry attempts to apply for the subscriber. |

**message/{topic}/ready**

This endpoint provides a list of the message ready to sent in Felis for a specific topic provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/topic/ready' \
  -H 'accept: application/json'
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
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
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
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
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
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
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
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

**subscribers/{connectionId}/messages**

This endpoint provides a list of the message that are been consumed for the connection id provided in route.

```
curl -X 'GET' \
  'https://localhost:7110/subscribers/AYhRMfzMA62BvJn3paMczQ/messages' \
  -H 'accept: application/json'
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
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

**subscribers/{connectionId}/messages/{topic}**

This endpoint provides a list of the message that are been consumed for the connection id provided in route and for a specific topic.

```
curl -X 'GET' \
  'https://localhost:7110/subscribers/AYhRMfzMA62BvJn3paMczQ/messages/topic' \
  -H 'accept: application/json'
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
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
builder.AddFelisRouter("username", "password"); => this line adds the FelisRouter with its related implementation for subscribers and dispatchers
app.UseFelisRouter(); => this line uses the implementations and endpoints
```

The signature of **AddFelisRouter** method is made of:

Parameter | Type    | Context                                                                                                                                                                                                                                          |
--- |---------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
username | string  | The FelisRouter username to apply the basic authentication.                                                                                                                                                                                         |
password | string | The FelisRouter password to apply the basic authentication.                                                                                                                                                                                            |

**Felis.Subscriber.Test**

To ease the testing process, I have implemented an ASP-NET minimal API application that exposes a publish endpoint.

This application contains a class, called TestConsumer, that implements the Consume<T> abstract class. It contains the Process(T entity) method implemented. It only shows how messages are intercepted.

**Program.cs**

Just add the following line of code:
```
builder.AddFelisSubscriber("https://username:password@localhost:7103", false 15, 5);
```

The signature of **AddFelisSubscriber** method is made of:

Parameter | Type    | Context                                                                                                                                                                                                                                            |
--- |---------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
connectionString | string  | The FelisRouter connection string that the subscriber must subscribe. It must contain the UserInfo to authenticate.                                                                                                                                |
unique | boolean | Tells to the FelisRouter if the subscriber is unique.                                                                                                                                                                                                |
pooledConnectionLifetimeMinutes | int     | The internal http client PooledConnectionLifetimeMinutes. Not mandatory. Default value is 15.                                                                                                                                                      |

**How do I use a subscriber?**

It is very simple. Just create a class that implements the IConsume<T> interface.
See two examples from GitHub here below:

***Sync mode***
```
using Felis.Subscriber.Test.Models;
using System.Text.Json;

namespace Felis.Subscriber.Test;

[Topic("Test", false, new(2))]
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
using Felis.Subscriber.Test.Models;

namespace Felis.Subscriber.Test;

[Topic("TestAsync", false, new(2))]
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

- Unit testing.
- Stress testing.
- Clusterization design.

  

