using Felis.Core;
using Felis.Core.Models;

namespace Felis.Router.Interfaces;

/// <summary>
/// This interface exposes the service methods to implements in router. It will listen the OnMessageReceived event, save on storage and forward it.
/// </summary>
internal interface IFelisRouterService
{
    Task<bool> Dispatch(Message? message, CancellationToken cancellationToken = default);

    Task<bool> Consume(ConsumedMessage? consumedMessage, CancellationToken cancellationToken = default);
    
    Task<bool> Error(ErrorMessage? errorMessage, CancellationToken cancellationToken = default);

    Task<List<Service>> GetConnectedServices(CancellationToken cancellationToken = default);
}