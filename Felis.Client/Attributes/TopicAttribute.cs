namespace Felis.Client.Attributes
{
	public sealed class TopicAttribute : Attribute
	{
		public string? Value { get; }

		public TopicAttribute(string? value)
		{
			Value = value;
		}
	}
}
