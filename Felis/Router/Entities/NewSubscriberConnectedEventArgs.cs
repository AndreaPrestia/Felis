using Felis.Common.Models;

namespace Felis.Router.Entities;

public class NewSubscriberConnectedEventArgs : EventArgs
{
    public Common.Models.Subscriber Subscriber { get; set; } = null!;
    public string ConnectionId { get; set; } = string.Empty;
}