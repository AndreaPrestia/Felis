using Felis.Common.Models;

namespace Felis.Router.Entities;

public class NewConsumerConnectedEventArgs : EventArgs
{
    public Consumer Consumer { get; set; } = null!;
    public string ConnectionId { get; set; } = string.Empty;
}