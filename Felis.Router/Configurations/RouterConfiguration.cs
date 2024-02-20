namespace Felis.Router.Configurations;

public sealed class RouterConfiguration
{
    public const string FelisRouter = nameof(FelisRouter);
    public MessageConfiguration? MessageConfiguration { get; set; }
    public ClusterConfiguration? ClusterConfiguration { get; set; }
}
