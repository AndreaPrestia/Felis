using System.Net;
using Felis.Common.Models;
using Felis.Router.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Hubs;

[Route("/felis/router")]
internal sealed class RouterHub : Hub
{
    private readonly ILogger<RouterHub> _logger;
    private readonly ConnectionService _connectionService;

    public RouterHub(ILogger<RouterHub> logger, ConnectionService connectionService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
    }

    public string SetConnectionId(List<TopicValue> topics)
    {
        try
        {
            var clientIp = Context.GetHttpContext()?.Connection.RemoteIpAddress;

            if (clientIp == null)
            {
                throw new InvalidOperationException($"No Ip address retrieve from Context {Context.ConnectionId}");
            }

            var clientHostname = Dns.GetHostEntry(clientIp).HostName;

            _connectionService.KeepSubscriberConnection(new Common.Models.Subscriber(clientHostname, clientIp.MapToIPv4().ToString(), topics), Context.ConnectionId);
           
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
            _connectionService.RemoveSubscriberConnections(connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
}