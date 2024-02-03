namespace Felis.Router.Configurations;

public sealed class FelisRouterConfiguration
{
    public const string FelisRouter = nameof(FelisRouter);
    public MessageConfiguration? MessageConfiguration { get; set; }
}
