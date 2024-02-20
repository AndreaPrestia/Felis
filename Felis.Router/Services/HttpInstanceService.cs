using Felis.Core.Models;
using System.Net;

namespace Felis.Router.Services
{
	internal sealed class HttpInstanceService
	{
		public Origin GetCurrentOrigin()
		{
			var ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
			var hostName = Dns.GetHostName();

			return new Origin(hostName, ipAddress);
		}
	}
}
