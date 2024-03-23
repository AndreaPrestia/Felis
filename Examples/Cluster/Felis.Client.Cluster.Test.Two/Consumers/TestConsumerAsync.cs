using System.Text.Json;
using Felis.Client.Attributes;
using Felis.Client.Cluster.Test.Two.Models;
using Felis.Core;

namespace Felis.Client.Cluster.Test.Two.Consumers;

[Topic("TestAsync")]
public class TestConsumerAsync : IConsume<TestModel>
{
    public async void Process(TestModel entity)
    {
        Console.WriteLine("Async mode");
        await Task.Run(() =>
            Console.WriteLine(JsonSerializer.Serialize(entity)));
    }
}