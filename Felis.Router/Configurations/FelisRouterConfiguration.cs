namespace Felis.Router.Configurations;

public sealed class FelisRouterConfiguration
{
    public const string FelisRouter = nameof(FelisRouter);
    public StorageConfiguration? StorageConfiguration { get; set; }
    public MessageConfiguration? MessageConfiguration { get; set; }
}
