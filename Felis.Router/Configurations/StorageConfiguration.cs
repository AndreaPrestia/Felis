namespace Felis.Router.Configurations;

public sealed class StorageConfiguration
{
    public string? Strategy { get; set; }
    public Dictionary<string, string>? Configurations { get; set; }
}