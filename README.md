# Felis
A message broker totally written in .NET, based on SignalR.

The Felis project is composed of two parts:

- **Router**, containing the logic for dispatching, storing and validating messages.
- **Client**, containing the logic of the client, that will consume a message by topic, using a specific entity contract.

**How can I use it?**

This repository provides two examples of usage:

- **Felis.Client.Test**
- **Felis.Router.Test**

**Felis.Router.Test**

An ASP-NET minimal API application, containing the five endpoints exposed by Felis.

The five endpoints are:

- dispatch
- consume
- error
- purge
- consumers

These endpoints are documented in the following page:

```
https://localhost:7103/swagger/index.html
```

**Dispatch**

This endpoint is used to dispatch a message with whatever contract in the payload, by topic, to every listener connected to Felis.

```
curl --location 'https://localhost:7103/dispatch' \
--header 'Content-Type: application/json' \
--data '{
    "header": {
        "topic": {
            "value": "test"
        },
        "services": []
    },
    "content": {
        "json": "{\"description\":\"Test\"}"
    }
}'
```

***Request***
Property | Type | Context |
--- | --- | --- |
header | object | the message header, containing the metadata of the message. |
header.topic | object | value object containing the topic of the message to dispatch. |
header.topic.value | string | the actual content of the topic of the message to dispatch. |
header.services |  array<service> | array of specific clients that should receive the message. | 
content | object | the message content. |
content.json | string | Json string of the message to dispatch. |

***Response***
Status code | Type | Context |
--- | --- | --- |
201 | CreatedResult object | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |

**Consume**

This endpoint informs Felis when a client successfully consumes a message. It is used to keep track of the operations (ACK).

```
curl --location 'https://localhost:7103/consume' \
--header 'Content-Type: application/json' \
--data '{
    "message": {
        "header": {
            "topic": {
                "value": "test"
            },
            "services": []
        },
        "content": {
            "json": "{\"description\":\"Test\"}"
        }
    },
    "service": {
        "ipAddress": "string",
        "hostname": "string",
        "isPublic": true,
	"topics": [{
                "value": "test"
            }
        ]
    },
}'
```

***Request***
Property | Type | Context |
--- | --- | --- |
message | object | The message entity used by Felis system. |
message.header | object | the message header, containing the metadata of the message. |
message.header.topic | object | value object containing the topic of the consumed message. |
message.header.topic.value | string | the actual value of the topic of the consumed message. |
message.content | object | the message content. |
message.content.json | string | Json string of the consumed message. |
service | object | The service entity that represents the client identity. |
service.ipAddress | string | The ipAddress property of the client. |
service.hostname | string | The hostname property of the client. |
service.isPublic | boolean | This property tells the router whether the client is configured to be discovered by other clients or not. |
service.topics | array<Topic> | This property contains the array of topics subscribed by a client. |

***Response***
Status code | Type | Context |
--- | --- | --- |
201 | CreatedResult object | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |

**Error**

This endpoint informs Felis when a client encounters errors while consuming a message. It is used to keep track of the operations (ACK).

```
curl --location 'https://localhost:7103/error' \
--header 'Content-Type: application/json' \
--data '{
    "message": {
        "header": {
            "topic": {
                "value": "test"
            },
            "services": []
        },
        "content": {
            "json": "{\"description\":\"Test\"}"
        }
    },
    "service": {
        "ipAddress": "string",
        "hostname": "string",
        "isPublic": true,
	"topics": [{
                "value": "test"
            }
        ]
    },
    "exception": {
        "targetSite": {
            "memberType": 1,
            "declaringType": "string",
            "reflectedType": "string",
            "module": {
                "assembly": "string",
                "moduleHandle": {}
            },
            "attributes": 0,
            "methodImplementationFlags": 0,
            "callingConvention": 1,
            "methodHandle": {
                "value": {}
            }
        },
        "innerException": "string",
        "helpLink": "string",
        "source": "string",
        "hResult": 0
    }
}'
```

***Request***
Property | Type | Context |
--- | --- | --- |
message | object | The message entity used by Felis system. |
message.header | object | the message header, containing the metadata of the message. |
message.header.topic | object | value object containing the topic of the message that throws an error. |
message.header.topic.value | string | the actual value of the topic of the message that throws an error. |
message.content | object | the message content. |
message.content.json | string | Json string of the message that throws an error. |
service | object | The service entity that represents the client identity. |
service.ipAddress | string | The ipAddress property of the client. |
service.hostname | string | The hostname property of the client. |
service.isPublic | boolean | This property tells the router whether the client is configured to be discovered by other clients or not. |
service.topics | array<Topic> | This property contains the array of topics subscribed by a client. |
exception | object | The .NET exception object that contains the occurred error. |

***Response***
Status code | Type | Context |
--- | --- | --- |
201 | CreatedResult object | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |

**Purge**

This endpoint tells the router to purge the queue for a specific topic. It is irreversible.

```
curl --location --request DELETE 'https://localhost:7103/purge/topic'
```

***Request***

Property | Type | Context |
--- | --- | --- |
topic | object | value object containing the topic of the message queue to purge. |
topic.value | string | the actual value of the topic of the message queue to purge. |

***Response***

Status code | Type | Context |
--- | --- | --- |
204 | NoContentRequest | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |

**Consumers**

This endpoint provides a list of the clients connected to Felis that consume a specific topic provided in the route. It exposes only the clients that are configured with the property **IsPublic** to **true**, which makes the clients discoverable.

```
curl --location 'https://localhost:7103/consumers/topic'
```

***Response***

```
[
   {
      "ipAddress":"192.168.1.1",
      "hostname":"host",
      "isPublic":true,
      "topics":[
         {
            "value":"topic"
         }
      ]
   }
]
```
This endpoint returns an array of clients.

Property | Type | Context |
--- | --- | --- |
ipAddress | string | The ipAddress property of the client. |
hostname | string | The hostname property of the client. |
isPublic | boolean | This property tells the router whether the client is configured to be discovered by other clients or not. |
topics | array<Topic> | This property contains the array of topics subscribed by a client. |

**Configuration**

Add this section to appsettings.json. 

```
"FelisRouter": {
    "MessageConfiguration": {
      "TimeToLiveMinutes": 5,
      "MinutesForEveryClean": 2,
      "MinutesForEveryRequeue": 2
    }
  }
```
The configuration is made of:

Property | Type | Context |
--- | --- | --- |
MessageConfiguration | object | The message configuration. |
MessageConfiguration.TimeToLiveMinutes | int | The TTL for a message in the router queue. |
MessageConfiguration.MinutesForEveryClean | int | It makes the queue cleaner run every N minutes. |
MessageConfiguration.MinutesForEveryRequeue | int | It makes the re-queue service run every N minutes. |

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
builder.AddFelisClient("https://localhost:7103", 15, 5);
```

The signature of **AddFelisClient** method is made of:

Parameter | Type | Context                                                                                                                                                                                                                                            |
--- | --- |----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
routerEndpoint | string | The FelisRouter endpoint that the client must subscribe.                                                                                                                                                                                           |
pooledConnectionLifetimeMinutes | int | The internal http client PooledConnectionLifetimeMinutes. Not mandatory. Default value is 15.                                                                                                                                                      |
maxAttempts | int | It tells the router the maximum number of attempts that should be made to resend a message in the error queue, according to the retry policy that you want to apply. All the attempts are logged in the router. Not mandatory, default value is 0. |

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

  

