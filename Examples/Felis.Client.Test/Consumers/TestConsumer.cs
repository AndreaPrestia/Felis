using System.Text.Json;
using Felis.Client.Attributes;
using Felis.Client.Test.Models;
using Felis.Core;

namespace Felis.Client.Test.Consumers;

[Topic("Test")]
public class TestConsumer : IConsume<TestModel>
{
    public void Process(TestModel entity)
    {
        Console.WriteLine("Sync mode");
        Console.WriteLine(JsonSerializer.Serialize(entity));
    }
}