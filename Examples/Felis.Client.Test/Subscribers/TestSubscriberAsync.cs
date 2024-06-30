using System.Text.Json;
using Felis.Client.Test.Models;
using Felis.Subscriber;
using Felis.Subscriber.Attributes;

namespace Felis.Client.Test.Subscribers;

[Topic("TestAsync")]
public class TestConsumerAsync : ISubscribe<TestModel>
{
    public async void Listen(TestModel entity)
    {
        Console.WriteLine("Async mode");
        await Task.Run(() =>
            Console.WriteLine(JsonSerializer.Serialize(entity)));
    }
}