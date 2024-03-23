using System.Text.Json;
using Felis.Client.Attributes;
using Felis.Client.Cluster.Test.Two.Models;
using Felis.Core;

namespace Felis.Client.Cluster.Test.Two.Consumers;

[Topic("Test")]
public class TestConsumer : IConsume<TestModel>
{
    public void Process(TestModel entity)
    {
        Console.WriteLine("Sync mode");
        Console.WriteLine(JsonSerializer.Serialize(entity));
    }
}