using Felis.Client.Test.Models;
using System.Text.Json;

namespace Felis.Client.Test
{
	[Topic("Test")]
	public class TestConsumer : IConsume<TestModel>
	{
		public async void Process(TestModel entity)
		{
			Console.WriteLine($"Sync mode {System.Environment.NewLine} {JsonSerializer.Serialize(entity)}");
		}
	}
}
