using Felis.Client.Attributes;
using Felis.Client.Cluster.Test.One.Models;
using Felis.Core;

namespace Felis.Client.Cluster.Test.One.Consumers;

[Topic("TestError")]
public class TestConsumerWithError : IConsume<TestModel>
{
    public void Process(TestModel entity)
    {
        throw new NotImplementedException("Example with exception in error queue");
    }
}