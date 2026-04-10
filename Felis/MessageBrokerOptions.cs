namespace Felis;

public class MessageBrokerOptions
{
    public string DataPath { get; set; } = "felis-data";
    public int ChannelCapacity { get; set; } = 100;
}
