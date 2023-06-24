# Felis
An experiment used to bring a message broker totally written in .NET 

The Felis project is composed of two parts:

- **Router**, containing the logic to dispatch , store and validate messages.
- **Client**, containing the logic of the client, that will consume a message by topic , using a specific entity contract.

**How can i use it?**

We have two examples:

- **Felis.Client.Test**
- **Felis.Router.Test**

**Felis.Router.Test**

An AspNet minimal api application, containing the two endpoints exposed by Felis.
The two endpoints are:

- Dispatch
- Consume

**Dispatch**

This endpoint is used to dispatch a message, on a topic , to every listener connected to Felis, with whatever contract in the payload :)

**Consume**

This endpoint says to Felis that a Consumer has consumed a message. It is used to keep track of the operations.

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

Just add this part in appsettings.json . This tells to the client which Felis router subscribe. It also must contains the client instance informations as service.
```
 "FelisClient": {
    "RouterEndpoint": "https://localhost:7103",
    "Service": {
      	"Id": "00000000-0000-0000-0000-000000000000",
      	"Name": "name",
      	"Host": "host",
      	"IsPublic": true
     }
  }
```

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

  

