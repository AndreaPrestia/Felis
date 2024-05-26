namespace Felis.Router.Enums;

public enum MessageSendStatus
{
    NothingToSend,
    MessageNotFound,
    MessageNotReady,
    MessageWithoutTopic,
    NoConnectionIdAvailableForTopic,
    MessageSent,
    MessageNotSent
}