using Felis.Core;
using Felis.Core.Models;

namespace Felis.Router.Interfaces;

/// <summary>
/// This interface exposes the service methods to implements in router. It will listen the OnMessageReceived event, save on storage and forward it.
/// </summary>
internal interface IFelisRouterService
{
    Task<bool> Dispatch(Topic topic, Message? message, CancellationToken cancellationToken = default);

    Task<bool> Consume(Guid id, ConsumedMessage? consumedMessage, CancellationToken cancellationToken = default);
    
    Task<bool> Error(Guid id, ErrorMessage? errorMessage, CancellationToken cancellationToken = default);

    Task<bool> Purge(Topic? topic, CancellationToken cancellationToken = default);
    
    Task<List<Service>> Consumers(Topic? topic, CancellationToken cancellationToken = default);
    
    Task<List<Message?>> MessageList(Topic? topic = null, CancellationToken cancellationToken = default);
    Task<List<ErrorMessage>> ErrorMessageList(Topic? topic = null, CancellationToken cancellationToken = default);
    Task<List<ConsumedMessage?>> ConsumedMessageList(ConnectionId connectionId, CancellationToken cancellationToken = default);
    Task<List<ConsumedMessage?>> ConsumedMessageList(ConnectionId connectionId, Topic topic, CancellationToken cancellationToken = default);
    Task<List<ConsumedMessage?>> ConsumedMessageList(Topic topic, CancellationToken cancellationToken = default);
}