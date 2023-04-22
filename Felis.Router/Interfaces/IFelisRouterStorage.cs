using Felis.Core;

namespace Felis.Router.Interfaces;

public interface IFelisRouterStorage
{
    void ConsumedMessageAdd(ConsumedMessage consumedMessage);

    void MessageAdd(Message message);

    void MessagePurge(string topic);

    List<Message> MessageList(string? topic = null);

    List<ConsumedMessage> ConsumedMessageList(Guid client);

    List<ConsumedMessage> ConsumedMessageList(string topic);
}