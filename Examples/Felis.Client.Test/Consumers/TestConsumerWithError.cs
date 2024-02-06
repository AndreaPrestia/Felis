using Felis.Client.Attributes;
using Felis.Client.Test.Models;
using Felis.Core;

namespace Felis.Client.Test.Consumers;

[Topic("TestError")]
public class TestConsumerWithError : IConsume<TestModel>
{
    public void Process(TestModel entity)
    {
        throw new NotImplementedException("Example with exception in error queue");
    }
}