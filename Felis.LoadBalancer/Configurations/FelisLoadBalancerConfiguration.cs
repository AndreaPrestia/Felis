namespace Felis.LoadBalancer.Configurations;

public class FelisLoadBalancerConfiguration
{
    public const string FelisLoadBalancer = nameof(FelisLoadBalancer);
    public List<string> Routers { get; set; } = new();
}