namespace Felis.Router.Interfaces
{
	public interface IFelisConnectionManager
	{
		void KeepUserConnection(Guid id, string connectionId);
		void RemoveUserConnection(string connectionId);
		List<string> GetUserConnections(Guid id);
	}
}
