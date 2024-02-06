using System.Net;
using Felis.Core;
using Felis.Core.Models;
using Felis.Router.Managers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Hubs;

[Route("/felis/router")]
internal sealed class FelisRouterHub : Hub
{
    private readonly ILogger<FelisRouterHub> _logger;
    private readonly FelisConnectionManager _felisConnectionManager;

    public FelisRouterHub(ILogger<FelisRouterHub> logger, FelisConnectionManager felisConnectionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _felisConnectionManager = felisConnectionManager ?? throw new ArgumentNullException(nameof(felisConnectionManager));
    }

    public string SetConnectionId(List<Topic> topics, string friendlyName)
    {
        try
        {
            var clientIp = Context.GetHttpContext()?.Connection.RemoteIpAddress;

            if (clientIp == null)
            {
                throw new InvalidOperationException($"No Ip address retrieve from Context {Context.ConnectionId}");
            }

            var clientHostname = Dns.GetHostEntry(clientIp).HostName;

            _felisConnectionManager.KeepServiceConnection(new Service(friendlyName, clientHostname, clientIp.ToString(), topics),
                new ConnectionId(Context.ConnectionId));
            return Context.ConnectionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return string.Empty;
        }
    }

    public void RemoveConnectionId(ConnectionId connectionId)
    {
        try
        {
            _felisConnectionManager.RemoveServiceConnections(connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
}