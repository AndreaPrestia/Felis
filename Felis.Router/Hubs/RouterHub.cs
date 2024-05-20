using System.Net;
using Felis.Core.Models;
using Felis.Router.Managers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Hubs;

[Route("/felis/router")]
internal sealed class RouterHub : Hub
{
    private readonly ILogger<RouterHub> _logger;
    private readonly ConnectionManager _felisConnectionManager;

    public RouterHub(ILogger<RouterHub> logger, ConnectionManager felisConnectionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _felisConnectionManager = felisConnectionManager ?? throw new ArgumentNullException(nameof(felisConnectionManager));
    }

    public string SetConnectionId(List<string> topics, string friendlyName)
    {
        try
        {
            var clientIp = Context.GetHttpContext()?.Connection.RemoteIpAddress;

            if (clientIp == null)
            {
                throw new InvalidOperationException($"No Ip address retrieve from Context {Context.ConnectionId}");
            }

            var clientHostname = Dns.GetHostEntry(clientIp).HostName;

            _felisConnectionManager.KeepConsumerConnection(new Consumer(friendlyName, clientHostname, clientIp.ToString(), topics),
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
            _felisConnectionManager.RemoveConsumerConnections(connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
}