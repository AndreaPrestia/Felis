using System.Text.Json;
using Felis.Client.Attributes;
using Felis.Client.Second.Test.Models;

namespace Felis.Client.Second.Test.Consumers;

[Topic("Test")]
public class TestConsumer : IConsume<TestModel>
{
    public void Process(TestModel entity)
    {
        Console.WriteLine("Sync mode");
        Console.WriteLine(JsonSerializer.Serialize(entity));
    }
}