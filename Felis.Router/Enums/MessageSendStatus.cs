namespace Felis.Router.Enums;

public enum MessageSendStatus
{
    MessageReady,
    NothingToSend,
    MessageNotFound,
    MessageNotReady,
    MessageWithoutTopic,
    NoConnectionIdAvailableForTopic,
    ConnectionIdNotFound,
    MessageSent,
    MessageNotSent
}