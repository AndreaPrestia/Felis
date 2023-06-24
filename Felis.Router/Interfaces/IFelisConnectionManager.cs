using Felis.Core.Models;

namespace Felis.Router.Interfaces
{
	internal interface IFelisConnectionManager
	{
		void KeepServiceConnection(Service service, string connectionId);
		void RemoveServiceConnections(Service service);
		List<string> GetServiceConnections(Service service);
		List<Service> GetConnectedServices();
	}
}
