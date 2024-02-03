using Felis.Core;
using Felis.Core.Models;

namespace Felis.Router.Interfaces;

public interface IFelisRouterStorage
{
    bool ConsumedMessageAdd(ConsumedMessage? consumedMessage);
    bool MessageAdd(Message? message);
    bool MessagePurge(Topic? topic);
    bool MessagePurge(int? timeToLiveMinutes);
    bool ErrorMessageAdd(ErrorMessage message);
	List<Message?> MessageList(Topic? topic = null);
	List<ErrorMessage> ErrorMessageList(Topic? topic = null);
	List<ConsumedMessage?> ConsumedMessageList(ConnectionId connectionId);
    List<ConsumedMessage?> ConsumedMessageList(Topic topic);
    List<ErrorMessage> ListMessagesToRequeue();
}