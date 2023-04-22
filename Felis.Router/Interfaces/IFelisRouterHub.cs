namespace Felis.Router.Interfaces;

/// <summary>
/// This interface exposes the hub methods to implements in router. It will listen the OnMessageReceived event, save on storage and forward it.
/// </summary>
public interface IFelisRouterHub
{
    void ListenForNewMessages(CancellationToken cancellationToken = default);

    void ListenForMessageStatus(CancellationToken cancellationToken = default);
}