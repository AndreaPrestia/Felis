using Felis.Common.Models;

namespace Felis.Subscriber.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class QueueAttribute : Attribute
{
    public string? Value { get; }
    public bool Unique { get; }
    public RetryPolicy? RetryPolicy { get; }

    public QueueAttribute(string? value, bool unique, int attempts = 0)
    {
        Value = value;
        Unique = unique;
        RetryPolicy = new RetryPolicy(attempts);
    }
}