using Felis.Client.Test.Models;
using Felis.Subscriber;
using Felis.Subscriber.Attributes;

namespace Felis.Client.Test.Subscribers;

[Topic("TestError")]
public class TestConsumerWithError : ISubscribe<TestModel>
{
    public void Listen(TestModel entity)
    {
        throw new NotImplementedException("Example with exception in error queue");
    }
}