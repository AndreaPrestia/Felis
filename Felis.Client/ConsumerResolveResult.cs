using System.Reflection;

namespace Felis.Client;

public class ConsumerResolveResult
{
    public bool Error { get; init; }
    public Exception? Exception { get; init; }
    public object? Consumer { get; init; }
    public Type? ConsumerType { get; init; }
    public Type? MessageType { get; init; }
    
    public MethodInfo? ProcessMethod { get; init; }
    
    public object? DeserializedEntity { get; set; }
}