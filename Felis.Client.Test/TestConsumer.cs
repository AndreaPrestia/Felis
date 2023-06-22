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
