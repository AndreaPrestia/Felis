namespace Felis.Core.Models;

public class Service
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Host { get; set; }
    public bool IsPublic { get; set; }
}

//TODO 

//add a service discovery part

//authorize requests if active

//add the service registration automatic

//add the service to service communication with the use of connection id, service id 

