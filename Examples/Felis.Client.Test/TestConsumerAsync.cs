using System.Text.Json;
using Felis.Client.Test.Models;

namespace Felis.Client.Test;

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