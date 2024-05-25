using Felis.Core.Models;
using Felis.Router.Entities;

namespace Felis.Router.Abstractions;

internal interface IRouterStorage
{
    MessageStatus ConsumedMessageAdd(ConsumedMessage? consumedMessage);
    MessageStatus ProcessedMessageAdd(ProcessedMessage? processedMessage);
    MessageStatus ReadyMessageAdd(MessageRequest? message);
    Message? MessageGet(Guid messageId);
    List<Message> ReadyMessageList(string? topic = null);
    MessageStatus SentMessageAdd(Guid messageId);
    List<Message> SentMessageList(string? topic = null);
    List<ConsumedMessage> ConsumedMessageListByConnectionId(string connectionId);
    List<ConsumedMessage> ConsumedMessageList(string topic);
    List<ConsumedMessage> ConsumedMessageList(string connectionId, string topic);
    int ReadyMessagePurge(string topic);
    MessageStatus ErrorMessageAdd(ErrorMessageRequest? message);
    List<ErrorMessage> ErrorMessageList(string? topic = null);
}