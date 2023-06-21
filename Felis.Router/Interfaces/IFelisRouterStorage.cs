using Felis.Core;
using Felis.Core.Models;

namespace Felis.Router.Interfaces;

public interface IFelisRouterStorage
{
    void ConsumedMessageAdd(ConsumedMessage consumedMessage);

    void MessageAdd(Message message);

    void MessagePurge(string topic);

    List<Message> MessageList(string? topic = null);

    List<ConsumedMessage> ConsumedMessageList(Client client);

    List<ConsumedMessage> ConsumedMessageList(Topic topic);
}