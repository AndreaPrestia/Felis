using System.Text.Json;
using Felis.Client.Test.Models;
using Felis.Subscriber;
using Felis.Subscriber.Attributes;

namespace Felis.Client.Test.Consumers;

[Queue("Test", true, 2)]
public class TestConsumerUnique : IConsume<TestModel>
{
	public async void Process(TestModel entity)
	{
		Console.WriteLine("Sync mode");
		Console.WriteLine(JsonSerializer.Serialize(entity));
	}
}