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
- services
- purge

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
        "name": "string",
        "host": "string",
        "isPublic": true
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
service.name | string | The name property of the client. |
service.host | string | The host property of the client. |
service.isPublic | boolean | This property tells the router whether the client is configured to be discovered by other clients or not. |

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
        "name": "string",
        "host": "string",
        "isPublic": true
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
service.name | string | The name property of the client. |
service.host | string | The host property of the client. |
service.isPublic | boolean | This property tells the router whether the client is configured to be discovered by other clients or not. |
exception | object | The .NET exception object that contains the occurred error. |

***Response***
Status code | Type | Context |
--- | --- | --- |
201 | CreatedResult object | When the request is successfully processed. |
400 | BadRequestResult | When a validation or something not related to the authorization process fails. |
401 | UnauthorizedResult | When an operation fails due to missing authorization. |
403 | ForbiddenResult | When an operation fails because it is not allowed in the context. |

**Services**

This endpoint provides a list of the clients connected to Felis. It exposes only the clients that are configured with the property **IsPublic** to **true**, which makes the clients discoverable.

```
curl --location 'https://localhost:7103/services'
```

***Response***

```
[
    {
        "name": "name",
        "host": "host",
        "isPublic": true
    }
]
```
This endpoint returns an array of clients.

Property | Type | Context |
--- | --- | --- |
name | string | The name property of the client. |
host | string | The host property of the client. |
isPublic | boolean | This property tells the router whether the client is configured to be discovered by other clients or not. |

**Purge**

This endpoint tells the router to purge the queue for a specific topic. It is irreversible.

```
curl --location --request DELETE 'https://localhost:7103/purge' \
--header 'Content-Type: application/json' \
--data '{
    "topic": {
        "value": "topic"
    }
}'
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

**Configuration**

Add this section to appsettings.json. 

```
"FelisRouter": {
    "MessageConfiguration": {
      "TimeToLiveMinutes": 5,
      "MinutesForEveryClean": 2,
      "MinutesForEveryRequeue": 2
    },
    "StorageConfiguration": {
      "Strategy": "InMemory",
      "Configurations": {
        "abc": "def"
      }
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
StorageConfiguration | object | The storage configuration. |
StorageConfiguration.TimeToLiveMinutes | string | The storage strategy. The available values are **InMemory** and **Persistent**. If the field is left empty or not provided, **InMemory** will be used by default. |
StorageConfiguration.Configurations | Dictionary<string,string> | Dictionary for storage configuration, to be used upon choosing **Persistent**. |

**Program.cs**

Add these two lines of code:

```
builder.AddFelisRouter(); => this line adds the FelisRouter with its related implementation for clients and dispatchers
app.UseFelisRouter(); => this line uses the implementations and endpoints
```

**Felis.Client.Test**

To ease the testing process, I have implemented an ASP-NET minimal API application that exposes a publish endpoint.

This application contains a class, called TestConsumer, that implements the Consume<T> abstract class. It contains the Process(T entity) method implemented. It only shows how messages are intercepted.

**Configuration**

Just add this section to appsettings.json. 

```
 "FelisClient": {
    "RouterEndpoint": "https://localhost:7103",
    "Service": {
      	"Name": "name",
      	"Host": "host",
      	"IsPublic": true
     },
     "RetryPolicy" : {
	"Attempts": 5
     },
     "Cache": {
      "SlidingExpiration": 3600,
      "AbsoluteExpiration": 3600,
      "MaxSizeBytes": 3000
     }
  }
```
The configuration is made of:

Property | Type | Context |
--- | --- | --- |
Router | object | The router configuration object. |
Router.Endpoint | string | The FelisRouter endpoint that the client must subscribe. |
Service | object | The client configuration. |
Service.Name | string | The client name. Paired with **host**, it provides its unique identity to FelisRouter. |
Service.Host | string | The client host. Paired with **name**, it provides its unique identity to FelisRouter. |
Service.IsPublic | boolean | This property tells the router whether the client is configured to be discovered by other clients or not. |
RetryPolicy | object | The retry policy configuration. |
RetryPolicy.Attempts | int | It tells the router the maximum number of attempts that should be made to resend a message in the error queue, according to the configured retry policy. All the attempts are logged in the router. |
Cache | object | The cache configuration. The cache applies to all the client consumers. |
Cache.SlidingExpiration | double | The SlidingExpiration for IMemoryCacheOptions. |
Cache.AbsoluteExpiration | double | The AbsoluteExpiration for IMemoryCacheOptions.|
Cache.MaxSizeBytes | long | The MaxSizeBytes that can reach the cache, used for IMemoryCacheOptions. |

**Program.cs**

For the ASP-NET core applications, add:
```
builder.AddFelisClientWeb();
```
For all the other .NET applications, add:
```
builder.AddFelisClient();
```

**How do I use a consumer?**

It is very simple. Just create a class that implements the Consume<T> abstract one.
See an example from GitHub here below:

```
using Felis.Client.Test.Models;
using System.Text.Json;

namespace Felis.Client.Test
{
	[Topic("Test")]
	public class TestConsumer : Consume<TestModel>
	{
		public override void Process(TestModel entity)
		{
			Console.WriteLine(JsonSerializer.Serialize(entity));
		}
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

  

