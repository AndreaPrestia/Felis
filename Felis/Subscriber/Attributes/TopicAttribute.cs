using Felis.Common.Models;

namespace Felis.Subscriber.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TopicAttribute : Attribute
{
	public string? Value { get; }

	public TopicAttribute(string? value)
	{
		Value = value;
	}
}