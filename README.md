# Felis
A message queue totally written in .NET, based on SignalR.

![FelisDiagram.jpg](Images%2FFelisDiagram.jpg.png)

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
- messages/publish
- messages/ack
- messages/nack
- messages/topics/{topic}/ready/purge
- messages/topics/{topic}/subscribers
- messages/topics/{topic}/ready
- messages/topics/{topic}/sent
- messages/topics/{topic}/error
- messages/topics/{topic}/consumed
- subscribers/{connectionId}/messages
- subscribers/{connectionId}/messages/topics/{topic}
- subscribers/{connectionId}/messages/queue/{queue}

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

**messages/publish**

This endpoint is used to dispatch a message with whatever contract in the payload, by queue, to the first available consumer connected to Felis.

```
curl -X 'POST' \
  'https://localhost:7110/messages/publish' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
  -d '{
    "header": {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "queue": "test"
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

**messages/ack**

This endpoint informs Felis when a consumer successfully consumes a message. It is used to keep track of the operations (ACK).

```
curl -X 'POST' \
  'https://localhost:7110/messages/ack' \
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

**messages/nack**

This endpoint informs Felis when a consumer encounters errors while consuming a message. It is used to keep track of the operations (ACK).
If no retry policy is provided in the Queue attribute, by the consumer, the message is not re-queued but goes in the dead letter storage.

```
curl -X 'POST' \
  'https://localhost:7110/messages/nack' \
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

**messages/topics/{topic}/ready/purge**

This endpoint tells the router to purge the ready message list for a specific topic. It is irreversible.

```
curl -X 'DELETE' \
  'https://localhost:7110/messages/topics/topic/purge' \
  -H 'accept: application/json'
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
  ```

***Request***

Property | Type | Context                                                     |
--- | --- |-------------------------------------------------------------|
topic | string | the actual value of the topic of the message list to purge. |

***Response***

Status code | Type | Context |
--- | --- | --- |
204 | NoContentRequest | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |
409 | Conflict | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

**messages/queues/{queue}/ready/purge**

This endpoint tells the router to purge the ready message list for a specific queue. It is irreversible.

```
curl -X 'DELETE' \
  'https://localhost:7110/messages/queues/queue/purge' \
  -H 'accept: application/json'
  -H 'Authorization: Basic dXNlcm5hbWU6cGFzc3dvcmQ=' \
  ```

***Request***

Property | Type | Context                                                      |
--- | --- |--------------------------------------------------------------|
queue | string | the actual value of the queue of the message queue to purge. |

***Response***

Status code | Type | Context |
--- | --- | --- |
204 | NoContentRequest | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |
409 | Conflict | When an operation fails because it conflicts with another one. |
500 | Internal server error | When an operation fails for another reason. |

**messages/topics/{topic}/subscribers**

This endpoint provides a list of the subscribers connected to Felis that consume a specific topic provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/topics/topic/subscribers' \
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
          "name": "topic"
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

**messages/queues/{queue}/subscribers**

This endpoint provides a list of the subscribers connected to Felis that consume a specific queue provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/queues/queue/subscribers' \
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
      "queues":[
      {
          "name": "queue",
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
queues | array<Queue> | This property contains the array of queues subscribed by the consumer.                               |
queues.name | string       | This property contains the queue name subscribed by the consumer                                     |
queues.unique | boolean      | This property tells the router whether the queue has a unique consumer registered in the subscriber. |
queues.retryPolicy | object       | This property tells the router whether the queue has a retry policy to apply for the subscriber.     |
queues.retryPolicy.attempts | int          | This property tells the router whether the queue has the retry attempts to apply for the subscriber. |

**message/topics/{topic}/ready**

This endpoint provides a list of the message ready to sent in Felis for a specific topic provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/topics/topic/ready' \
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


**message/queues/{queue}/ready**

This endpoint provides a list of the message ready to sent in Felis for a specific queue provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/queues/queue/ready' \
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
      "queue": "string",
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
header.topic | string | the actual value of the queue of the message ready to be sent. |
content | object | the message content. |
content.payload | string | Json string of the message ready to be sent. |

**message/topics/{topic}/sent**

This endpoint provides a list of the message sent in Felis for a specific topic provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/topics/topic/sent' \
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

**message/queues/{queue}/sent**

This endpoint provides a list of the message sent in Felis for a specific queue provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/queues/queue/sent' \
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
      "queue": "string",
      "timestamp": 0
    },
    "content": {
      "payload": "string"
    }
  }
]
```
This endpoint returns an array of message.

Property | Type | Context                                                        |
--- | --- |----------------------------------------------------------------|
header | object | the message header, containing the metadata of the message.    |
header.id | guid | the message global unique identifier.                          |
header.queue | string | the actual value of the queue of the message ready to be sent. |
content | object | the message content.                                           |
content.payload | string | Json string of the message ready to be sent.                   |

**messages/topics/{topic}/error**

This endpoint provides a list of the message that are gone in the error queue for a specific topic provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/topics/topic/error' \
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

**messages/queues/{queue}/error**

This endpoint provides a list of the message that are gone in the error queue for a specific queue provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/queues/queue/error' \
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
                "queue": "string"
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

**messages/topics/{topic}/consumed**

This endpoint provides a list of the message that are been consumed for a specific topic provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/topics/topic/consumed' \
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

**messages/queues/{queue}/consumed**

This endpoint provides a list of the message that are been consumed for a specific queue provided in the route.

```
curl -X 'GET' \
  'https://localhost:7110/messages/queues/queue/consumed' \
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
                "topic": "string",
                "queue": "string"
            }
        },
        "connectionId": "AYhRMfzMA62BvJn3paMczQ",
	    "timestamp": 0
    }
]
```
This endpoint returns an array of messages that are consumed, with related connection id and timestamp.

Property | Type | Context                                                       |
--- | --- |---------------------------------------------------------------|
message | object | The message entity used by Felis system.                      |
message.header | object | the message header, containing the metadata of the message.   |
message.header.id | guid | the message global unique identifier.                         |
message.header.topic | string | the actual value of the topic of the message consumed.        |
message.header.queue | string | the actual value of the queue of the message consumed.        |
message.content | object | the message content.                                          |
message.content.payload | string | Json string of the message consumed.                          |
connectionId | string | the actual value of the connectionId of the message consumed. |
timestamp | long | The unix time in milliseconds that provides the consume time. |

**subscribers/{connectionId}/messages/topics/{topic}**

This endpoint provides a list of the message that are been consumed for the connection id provided in route and for a specific topic.

```
curl -X 'GET' \
  'https://localhost:7110/subscribers/AYhRMfzMA62BvJn3paMczQ/messages/topics/topic' \
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

**subscribers/{connectionId}/messages/queues/{queue}**

This endpoint provides a list of the message that are been consumed for the connection id provided in route and for a specific topic.

```
curl -X 'GET' \
  'https://localhost:7110/subscribers/AYhRMfzMA62BvJn3paMczQ/messages/queues/queue' \
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
                "queue": "string"
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
builder.AddFelisSubscriber("https://username:password@localhost:7103", 15);
```

The signature of **AddFelisSubscriber** method is made of:

Parameter | Type    | Context                                                                                                                                                                                                                                            |
--- |---------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
connectionString | string  | The FelisRouter connection string that the subscriber must subscribe. It must contain the UserInfo to authenticate.                                                                                                                                |
unique | boolean | Tells to the FelisRouter if the subscriber is unique.                                                                                                                                                                                                |
pooledConnectionLifetimeMinutes | int     | The internal http client PooledConnectionLifetimeMinutes. Not mandatory. Default value is 15.                                                                                                                                                      |

**How do I use a subscriber?**

It is very simple. Just create a class that implements the IConsume<T> and/or ISubscribe<T> interfaces.
See two examples from GitHub here below:

***Queue***

The queue provides ack and nack to the router and can be defined with a RetryPolicy and the consumer as unique.
The queue attribute define a 1-1 consumer producer ratio.
To consume a message for a queue you have to define a class that implement IConsume<T> with a queue attribute defined containing the queue name and eventually the true/false values that defines if the consumer is unique and the eventual retry policy for the consume.

****Sync mode****

It defines a sync consumer, for queue "Test", without be defined as unique with a retry policy of two times if failed.

```
using Felis.Subscriber.Test.Models;
using System.Text.Json;

namespace Felis.Subscriber.Test;

[Queue("Test", false, 2)]
public class TestConsumer : IConsume<TestModel>
{
	public async void Process(TestModel entity)
	{
		Console.WriteLine("Sync mode");
		Console.WriteLine(JsonSerializer.Serialize(entity));
	}
}
```

****Async mode****

It defines an async consumer, for queue "Test", without be defined as unique with a retry policy of two times if failed.

```
using System.Text.Json;
using Felis.Subscriber.Test.Models;

namespace Felis.Subscriber.Test;

[Queue("TestAsync", false, 2)]
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

****Unique mode****

It defines a sync consumer, for queue "Test", defined as unique with a retry policy of two times if failed.

```
using Felis.Subscriber.Test.Models;
using System.Text.Json;

namespace Felis.Subscriber.Test;

[Queue("Test", true, 2)]
public class TestConsumerUnique : IConsume<TestModel>
{
	public async void Process(TestModel entity)
	{
		Console.WriteLine("Sync mode");
		Console.WriteLine(JsonSerializer.Serialize(entity));
	}
}
```

***Topic***

The topic listens to a message that is broadcasted. No ack is done. No unique listener can be defined.
To listen a message for a topic you have to define a class that implement ISubscribe<T> with a topic attribute defined containing the topic name.

****Sync mode****

It defines a sync subscriber, for topic "Test".

```
using Felis.Subscriber.Test.Models;
using System.Text.Json;

namespace Felis.Subscriber.Test;

[Topic("Test")]
public class TestSubscriber : ISubscribe<TestModel>
{
	public async void Listen(TestModel entity)
	{
		Console.WriteLine("Sync mode");
		Console.WriteLine(JsonSerializer.Serialize(entity));
	}
}
```

****Async mode****

It defines an async subscriber, for topic "Test".

```
using System.Text.Json;
using Felis.Subscriber.Test.Models;

namespace Felis.Subscriber.Test;

[Topic("TestAsync")]
public class TestSubscriberAsync : ISubscribe<TestModel>
{
    public async void Listen(TestModel entity)
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

  

