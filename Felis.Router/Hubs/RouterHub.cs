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
    private readonly ConnectionManager _connectionManager;

    public RouterHub(ILogger<RouterHub> logger, ConnectionManager connectionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
    }

    public string SetConnectionId(List<string> topics, bool unique)
    {
        try
        {
            var clientIp = Context.GetHttpContext()?.Connection.RemoteIpAddress?.MapToIPv4();

            if (clientIp == null)
            {
                throw new InvalidOperationException($"No Ip address retrieve from Context {Context.ConnectionId}");
            }

            var clientHostname = Dns.GetHostEntry(clientIp).HostName;

            _connectionManager.KeepConsumerConnection(new Consumer(clientHostname, clientIp.ToString(), topics, unique), Context.ConnectionId);
            return Context.ConnectionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return string.Empty;
        }
    }

    public void RemoveConnectionId(string connectionId)
    {
        try
        {
            _connectionManager.RemoveConsumerConnections(connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
}