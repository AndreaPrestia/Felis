using Felis.Core;

namespace Felis.Router.Interfaces;

/// <summary>
/// This interface exposes the service methods to implements in router. It will listen the OnMessageReceived event, save on storage and forward it.
/// </summary>
public interface IFelisRouterService
{
    Task<bool> Dispatch(Message message, CancellationToken cancellationToken = default);

    Task<bool> Consume(ConsumedMessage consumedMessage, CancellationToken cancellationToken = default);
}