using Felis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Felis.Router.Abstractions;

internal interface IRouterStorage
{
    bool ConsumedMessageAdd(ConsumedMessage? consumedMessage);
    bool ReadyMessageAdd(Message? message);
    Message? ReadyMessageGet();
    List<Message?> ReadyMessageList(Topic? topic = null);
    bool SentMessageAdd(Message? message);
    List<Message?> SentMessageList(Topic? topic = null);
    List<ConsumedMessage?> ConsumedMessageList(ConnectionId connectionId);
    List<ConsumedMessage?> ConsumedMessageList(Topic topic);
    List<ConsumedMessage?> ConsumedMessageList(ConnectionId connectionId, Topic topic);
    bool ReadyMessagePurge(Topic? topic);
    bool ReadyMessagePurge(int? timeToLiveMinutes);
    bool ErrorMessageAdd(ErrorMessage? message);
    ErrorMessage? ErrorMessageGet();
    List<ErrorMessage?> ErrorMessageList(Topic? topic = null);
}