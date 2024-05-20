using Felis.Core.Models;

namespace Felis.Router.Abstractions;

internal interface IRouterStorage
{
    bool ConsumedMessageAdd(ConsumedMessage? consumedMessage);
    bool ReadyMessageAdd(Message? message);
    Message? ReadyMessageGet();
    List<Message> ReadyMessageList(string? topic = null);
    bool SentMessageAdd(Message? message);
    List<Message> SentMessageList(string? topic = null);
    List<ConsumedMessage> ConsumedMessageList(ConnectionId connectionId);
    List<ConsumedMessage> ConsumedMessageList(string topic);
    List<ConsumedMessage> ConsumedMessageList(ConnectionId connectionId, string topic);
    bool ReadyMessagePurge(string topic);
    bool ReadyMessagePurge(int? timeToLiveMinutes);
    bool ErrorMessageAdd(ErrorMessageRequest? message);
    ErrorMessage? ErrorMessageGet();
    List<ErrorMessage> ErrorMessageList(string? topic = null);
}