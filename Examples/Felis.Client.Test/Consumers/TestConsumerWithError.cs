using Felis.Client.Test.Models;
using Felis.Subscriber;
using Felis.Subscriber.Attributes;

namespace Felis.Client.Test.Consumers;

[Topic("TestError", false)]
public class TestConsumerWithError : IConsume<TestModel>
{
    public void Process(TestModel entity)
    {
        throw new NotImplementedException("Example with exception in error queue");
    }
}