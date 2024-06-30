using System.Text.Json;
using Felis.Client.Test.Models;
using Felis.Subscriber;
using Felis.Subscriber.Attributes;

namespace Felis.Client.Test.Subscribers;

[Topic("Test")]
public class TestSubscriber : ISubscribe<TestModel>
{
    public void Listen(TestModel entity)
    {
        Console.WriteLine("Sync mode");
        Console.WriteLine(JsonSerializer.Serialize(entity));
    }
}