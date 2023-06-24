using System.Security.Cryptography;

namespace Felis.Core.Models;

public record Service(string? Name, string? Host, bool IsPublic);

//TODO 

//add a service discovery part

//authorize requests if active

//add the service registration automatic

//add the service to service communication with the use of connection id, service id 