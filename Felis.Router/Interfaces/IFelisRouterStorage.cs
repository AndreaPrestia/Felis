using Felis.Core;
using Felis.Core.Models;

namespace Felis.Router.Interfaces;

public interface IFelisRouterStorage
{
    void ConsumedMessageAdd(ConsumedMessage? consumedMessage);

    void MessageAdd(Message? message);

    void MessagePurge(Topic? topic);

	void ErrorMessageAdd(ErrorMessage? message);

	List<Message?> MessageList(Topic? topic = null);
	List<ErrorMessage?> ErrorMessageList(Topic? topic = null, long? start = null, long? end = null);

	List<ConsumedMessage?> ConsumedMessageList(Client client);

    List<ConsumedMessage?> ConsumedMessageList(Topic topic);
}