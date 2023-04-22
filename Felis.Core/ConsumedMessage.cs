namespace Felis.Core;

public record ConsumedMessage
{
    public Message Message { get; }
    public Guid Client { get; set; }
    public long Timestamp { get; } = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();

    private ConsumedMessage(Message message, Guid client)
    {
        Message = message;
        Client = client;
    }

    public static ConsumedMessage From(Message message, Guid client)
    {
        return new ConsumedMessage(message, client);
    }
}