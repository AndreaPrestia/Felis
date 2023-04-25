using Felis.Core;

namespace Felis.Router.Interfaces;

/// <summary>
/// This interface exposes the hub methods to implements in router. It will listen the OnMessageReceived event, save on storage and forward it.
/// </summary>
internal interface IFelisRouterHub
{
    Task<bool> Dispatch(Message message, CancellationToken cancellationToken = default);

    Task<bool> Consume(ConsumedMessage consumedMessage, CancellationToken cancellationToken = default);
}