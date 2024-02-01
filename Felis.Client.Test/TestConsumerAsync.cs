using System.Text.Json;
using Felis.Client.Test.Models;

namespace Felis.Client.Test;

[Topic("TestAsync")]
public class TestConsumerAsync : IConsume<TestModel>
{
    public async void Process(TestModel entity)
    {
        await Task.Run(() => Console.WriteLine($"Async mode {System.Environment.NewLine} {JsonSerializer.Serialize(entity)}"));
    }
}