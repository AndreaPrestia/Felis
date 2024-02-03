using Felis.Core;
using Felis.Core.Models;

namespace Felis.Router.Interfaces
{
	internal interface IFelisConnectionManager
	{
		void KeepServiceConnection(Service service, ConnectionId connectionId);
		void RemoveServiceConnections(ConnectionId connectionId);
		List<ConnectionId> GetServiceConnections(Service service);
		List<Service> GetConnectedServices();
	}
}
