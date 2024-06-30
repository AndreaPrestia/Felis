using System.Reflection;

namespace Felis.Subscriber.Resolvers;

internal sealed class SubscriberResolverResult
{
    private SubscriberResolverResult(object? subscriber, Type? consumerType, Type? messageType, MethodInfo? processMethod,
        object? deserializedEntity)
    {
        Subscriber = subscriber;
        ConsumerType = consumerType;
        MessageType = messageType;
        ProcessMethod = processMethod;
        DeserializedEntity = deserializedEntity;
    }

    private SubscriberResolverResult(Exception? exception)
    {
        Exception = exception;
        Error = true;
    }

    public bool Error { get; init; }
    public Exception? Exception { get; init; }
    public object? Subscriber { get; init; }
    public Type? ConsumerType { get; init; }
    public Type? MessageType { get; init; }
    public MethodInfo? ProcessMethod { get; init; }
    public object? DeserializedEntity { get; set; }

    internal static SubscriberResolverResult Ok(object? consumer, Type? consumerType, Type? messageType,
        MethodInfo? processMethod,
        object? deserializedEntity) => new(consumer, consumerType, messageType,
        processMethod,
        deserializedEntity);

    internal static SubscriberResolverResult Ko(Exception? exception) => new(exception);
}