using Felis.Core.Models;

namespace Felis.Router.Abstractions;

internal interface IRouterStorage
{
    bool ConsumedMessageAdd(ConsumedMessage? consumedMessage);
    bool ProcessedMessageAdd(ProcessedMessage? processedMessage);
    bool ReadyMessageAdd(Message? message);
    Message? ReadyMessageGet();
    List<Message> ReadyMessageList(string? topic = null);
    bool SentMessageAdd(Message? message);
    List<Message> SentMessageList(string? topic = null);
    List<ConsumedMessage> ConsumedMessageListByConnectionId(string connectionId);
    List<ConsumedMessage> ConsumedMessageList(string topic);
    List<ConsumedMessage> ConsumedMessageList(string connectionId, string topic);
    bool ReadyMessagePurge(string topic);
    bool ErrorMessageAdd(ErrorMessageRequest? message);
    List<ErrorMessage> ErrorMessageList(string? topic = null);
}