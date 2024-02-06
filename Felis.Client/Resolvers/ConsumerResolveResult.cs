using System.Reflection;

namespace Felis.Client.Resolvers;

internal class ConsumerResolveResult
{
    private ConsumerResolveResult(object? consumer, Type? consumerType, Type? messageType, MethodInfo? processMethod,
        object? deserializedEntity)
    {
        Consumer = consumer;
        ConsumerType = consumerType;
        MessageType = messageType;
        ProcessMethod = processMethod;
        DeserializedEntity = deserializedEntity;
    }

    private ConsumerResolveResult(Exception? exception)
    {
        Exception = exception;
        Error = true;
    }

    public bool Error { get; init; }
    public Exception? Exception { get; init; }
    public object? Consumer { get; init; }
    public Type? ConsumerType { get; init; }
    public Type? MessageType { get; init; }
    public MethodInfo? ProcessMethod { get; init; }
    public object? DeserializedEntity { get; set; }

    internal static ConsumerResolveResult Ok(object? consumer, Type? consumerType, Type? messageType,
        MethodInfo? processMethod,
        object? deserializedEntity) => new(consumer, consumerType, messageType,
        processMethod,
        deserializedEntity);

    internal static ConsumerResolveResult Ko(Exception? exception) => new(exception);
}