using Felis.Common.Models;

namespace Felis.Subscriber.Attributes;

public sealed class TopicAttribute : Attribute
{
	public string? Value { get; }
	public bool Unique { get; }
	public RetryPolicy? RetryPolicy { get; }

	public TopicAttribute(string? value, bool unique, RetryPolicy? retryPolicy)
	{
		Value = value;
		Unique = unique;
		RetryPolicy = retryPolicy;
	}
}