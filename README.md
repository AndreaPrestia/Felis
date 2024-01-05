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
service.isPublic | boolean | This property states whether the client is configured to be discovered by other clients or not. |

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
message.header | object | the message header, containing the metadata of the message |
message.header.topic | object | value object containing the topic of the message that throws an error. |
message.header.topic.value | string | the value content of the topic of the message that throws an error. |
message.header.service | array<service> | array of service used to dispatch the message to a set of specific targets | 
message.content | object | the message content. |
message.content.json | string | Jsonized string of the message that throws an error. |
service | object | The service entity that represent the identity of the consumer. |
service.name | string | The service entity name of the consumer. |
service.host | string | The service entity host of the consumer. |
service.isPublic | boolean | Says if the consumer is configured to be discovered by other services or not. |
exception | object | The .NET exception object that contains the error occurred. |

***Response***
Status code | Type | Context |
--- | --- | --- |
201 | CreatedResult object | When everything goes well. |
400 | BadRequestResult | When a validation or something not related to the authorization part fails. |
401 | UnauthorizedResult | When an operation fails for missing authorization. |
403 | ForbiddenResult | When an operation fails because not valid in the context. |

**Services**

This endpoint says which services are connected to Felis. It exposes only the services that are configured with the property **IsPublic** to **true**. So these services are discoverable.

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
This endpoints returns an array of service entities.

Property | Type | Context |
--- | --- | --- |
name | string | The service entity name of the consumer. |
host | string | The service entity host of the consumer. |
isPublic | boolean | Says if the consumer is configured to be discovered by other services or not. |

**Purge**

This endpoints tells to the router to purge the queue for a specific Topic. It is irreversible.

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
topic.value | string | the value content of the topic of the message queue to purge. |

***Response***

Status code | Type | Context |
--- | --- | --- |
204 | NoContentRequest | When everything goes well. |
400 | BadRequestResult | When a validation or something not related to the authorization part fails. |
401 | UnauthorizedResult | When an operation fails for missing authorization. |
403 | ForbiddenResult | When an operation fails because not valid in the context. |

**Configuration**

Add this part in appsettings.json. 

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
The configuration is composed of:

Property | Type | Context |
--- | --- | --- |
MessageConfiguration | object | The configuration about the message part. |
MessageConfiguration.TimeToLiveMinutes | int | The TTL for a message in the router queue. |
MessageConfiguration.MinutesForEveryClean | int | Says every N minute that the queue cleaner has to run. |
MessageConfiguration.MinutesForEveryRequeue | int | Says every N minute that the re-queue service has to run. |
StorageConfiguration | object | The configuration about the storage part. |
StorageConfiguration.TimeToLiveMinutes | string | The storage strategy to use. The available values are **InMemory** and **Persistent**. If empty or not provided the **InMemory** one will be used. |
StorageConfiguration.Configurations | Dictionary<string,string> | Dictionary for storage configuration to use when **Persistent** strategy is choosen. |

**Program.cs**

Add this two lines:

```
builder.AddFelisRouter(); => this adds the FelisRouter with related implementation for services and dispatchers
app.UseFelisRouter(); => this uses the implementations
```

**Felis.Client.Test**

For sake of simplicity i have implemented an AspNet minimal api application, that exposes a publish endpoint, to facilitate the tests.
This can be used as whatever C# application you want.

It implements a class , called TestConsumer that implements the Consume<T> abstract class. It contains the Process(T entity) method implemented. It is only implemented to see how it intercepts messages.

**Configuration**

Just add this part in appsettings.json. 

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
The configuration is composed of:

Property | Type | Context |
--- | --- | --- |
Router | object | The router configuration object. |
Router.Endpoint | string | The endpoint where the client must subscribe. It's the router. |
Service | object | The service entity , representing the configuration as service of the Felis client. |
Service.Name | string | The service name. Paired with **host** gives the unique identity on the Felis router. |
Service.Host | string | The service host. Paired with **name** gives the unique identity on the Felis router. |
Service.IsPublic | boolean | Says, to the router , if this server istance can be reached and discovered by other services connected to Felis router. |
RetryPolicy | object | The object containing the retry policy for messages in the Felis client instance |
RetryPolicy.Attempts | int | Says, to the router , till the max number of attempts have to resend that specific message with the retry policy configured in the Felis client. The attempts are logged in the Router. |
Cache | object | The object containing the cache configuration part for Felis client, used to cache the consumers, to avoid the reflection part everytime. |
Cache.SlidingExpiration | double | The SlidingExpiration for IMemoryCacheOptions used. |
Cache.AbsoluteExpiration | double | The AbsoluteExpiration for IMemoryCacheOptions used.|
Cache.MaxSizeBytes | long | The MaxSizeBytes that can reach the cache, used for IMemoryCacheOptions used. |

**Program.cs**

For ASP NET core application add:
```
builder.AddFelisClientWeb();
```
For all the others .NET application add:
```
builder.AddFelisClient();
```

**How to use a consumer?**

It's very simple, just create a class that implements the Consume<T> abstract one.
Here an example in GitHub:

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

This is an experiment , it doesn't have the claim to be something like a game changer :D 

There a lot of things to do.

**TODO**

- Implement a mechanism of persistance storage of message in router, now are persisted in memory, it's not efficient.
- Implement an authorization mechanism to use it in public networks.
- Code cleanup and a lot of other things that now i don't remember.
- Add unit testing.

  

