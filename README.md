# Felis
An experiment used to bring a message broker totally written in .NET , based on SignalR.

The Felis project is composed of two parts:

- **Router**, containing the logic to dispatch , store and validate messages.
- **Client**, containing the logic of the client, that will consume a message by topic , using a specific entity contract.

**How can i use it?**

We have two examples:

- **Felis.Client.Test**
- **Felis.Router.Test**

**Felis.Router.Test**

An AspNet minimal api application, containing the four endpoints exposed by Felis.
The two endpoints are:

- dispatch
- consume
- error
- services

These endpoints are documented in the page

```
https://localhost:7103/swagger/index.html
```

**Dispatch**

This endpoint is used to dispatch a message, on a topic , to every listener connected to Felis, with whatever contract in the payload.

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
        "json": "{\"description\":\"Test\"}",
        "type": "Felis.Client.Test.TestModel",
    }
}'
```

***Request***
Property | Type | Context |
--- | --- | --- |
header | object | the message header, containing the metadata of the message |
header.topic | object | value object containing the topic of the message to dispatch. |
header.topic.value | string | the value content of the topic to dispatch. |
header.services |  array<service> | array of service hosts to dispatch the message to a set of specific targets | 
content | object | the message content. |
content.json | string | Jsonized string of the message to dispatch. |
content.type | string | the .NET type of the interface to use to deserialize the message to dispatch. It is used by the Consumer for deserialize.|

***Response***
Status code | Type | Context |
--- | --- | --- |
201 | CreatedResult object | When everything goes well. |
400 | BadRequestResult | When a validation or something not related to the authorization part fails. |
401 | UnauthorizedResult | When an operation fails for missing authorization. |
403 | ForbiddenResult | When an operation fails because not valid in the context. |

**Consume**

This endpoint says to Felis that a service has consumed a message. It is used to keep track of the operations.

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
            "json": "{\"description\":\"Test\"}",
            "type": "Felis.Client.Test.TestModel",
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
message.header | object | the message header, containing the metadata of the message |
message.header.topic | object | value object containing the topic of the message consumed. |
message.header.topic.value | string | the value content of the topic of the message consumed. |
message.header.services | array<service> | array of service used to dispatch the message to a set of specific targets | 
message.content | object | the message content. |
message.content.json | string | Jsonized string of the message consumed. |
message.content.type | string | the .NET type of the interface used to deserialize the message consumed. It is used by the Consumer for deserialize.|
service | object | The service entity that represent the identity of the consumer. |
service.name | string | The service entity name of the consumer. |
service.host | string | The service entity host of the consumer. |
service.isPublic | boolean | Says if the consumer is configured to be discovered by other services or not. |

***Response***
Status code | Type | Context |
--- | --- | --- |
201 | CreatedResult object | When everything goes well. |
400 | BadRequestResult | When a validation or something not related to the authorization part fails. |
401 | UnauthorizedResult | When an operation fails for missing authorization. |
403 | ForbiddenResult | When an operation fails because not valid in the context. |

**Error**

This endpoint says to Felis that a Consumer has encontered and error consuming a message. It is used to keep track of the operations.

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
            "json": "{\"description\":\"Test\"}",
            "type": "Felis.Client.Test.TestModel",
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
message.content.type | string | the .NET type of the interface used to deserialize the message that throws an error. It is used by the Consumer for deserialize.|
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

**Configuration**

Currently we don'have one. I have to add something for storage, now there is a dummy in memory one :)

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
     }
  }
```
The configuration is composed of:

- RouterEndpoint, representing the endpoint where the client must subscribe. It's the router.
- Service, the object representing the service descriptor of the Felis client.
  	- Name , the service name.
  	- Host, the host of the service instance.
  	- IsPublic, says, to the router , if this server istance can be reached and discovered by other services connected to Felis router.
 
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
- Implement a mechanism of cleanup of the storage, now the in-memory are not be deleted after a TTL to provide in configuration.
- Implement an authorization mechanism to use it in public networks.
- Code cleanup and a lot of other things that now i don't remember.
- Add a mechanism of public and private key header validation.
- Rewrite the message entity separating the payload of the message from the header part.

  

