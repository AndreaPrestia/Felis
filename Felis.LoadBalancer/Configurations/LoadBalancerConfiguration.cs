namespace Felis.LoadBalancer.Configurations;

public class LoadBalancerConfiguration
{
    public const string FelisLoadBalancer = nameof(FelisLoadBalancer);
    public List<string> Routers { get; set; } = new();
}