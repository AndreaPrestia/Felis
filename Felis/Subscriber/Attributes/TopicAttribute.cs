namespace Felis.Subscriber.Attributes;

public sealed class TopicAttribute : Attribute
{
	public string? Value { get; }
	public bool Unique { get; }

	public TopicAttribute(string? value, bool unique)
	{
		Value = value;
		Unique = unique;
	}
}