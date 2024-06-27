using Felis.Common.Models;

namespace Felis.Subscriber.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TopicAttribute : Attribute
{
	public string? Value { get; }
	public bool Unique { get; }
	public RetryPolicy? RetryPolicy { get; }

	public TopicAttribute(string? value, bool unique, int attempts = 0)
	{
		Value = value;
		Unique = unique;
		RetryPolicy = new RetryPolicy(attempts);
	}
}