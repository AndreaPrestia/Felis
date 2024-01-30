using System.Reflection;
using System.Text.Json;
using Felis.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Felis.Client;

public sealed class ConsumerResolver
{
    private readonly IServiceProvider _serviceProvider;

    public ConsumerResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public ConsumerResolveResult ResolveConsumerByTopic(KeyValuePair<Topic, Type> topicType, string? messagePayload)
    {
        try
        {
           return GetConsumer(topicType, messagePayload);
        }
        catch (Exception ex)
        {
            return new ConsumerResolveResult()
            {
                Error = true,
                Exception = ex
            };
        }
    }
    
    public Dictionary<Topic, Type> GetTypesForTopics()
    {
        var topicTypes = AppDomain.CurrentDomain.GetAssemblies()
            .First(x => x.GetName().Name == AppDomain.CurrentDomain.FriendlyName).GetTypes().Where(t =>
                t.BaseType?.FullName != null
                && t.BaseType.FullName.Contains("Felis.Client.Consume") &&
                t is { IsInterface: false, IsAbstract: false }).SelectMany(t =>
                t.GetCustomAttributes<TopicAttribute>()
                    .Select(x => new KeyValuePair<Topic, Type>(new Topic(x.Value), t)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        if (topicTypes == null)
        {
            throw new InvalidOperationException("Not found implementation of Consumer for any topic");
        }

        return topicTypes;
    }
    
    private ConsumerResolveResult GetConsumer(KeyValuePair<Topic, Type> topicType, string? messagePayload)
    {
        if (topicType.Key == null)
        {
            throw new ArgumentNullException(nameof(topicType.Key));
        }
        
        if (topicType.Value == null)
        {
            throw new ArgumentNullException(nameof(topicType.Value));
        }

        var scope = _serviceProvider.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var service = provider.GetService(topicType.Value);

        if (service == null)
        {
            throw new ApplicationException($"No consumer registered for topic {topicType.Key.Value}");
        }

        var processParameterInfo = topicType.Value.GetMethod("Process")?.GetParameters().FirstOrDefault();

        if (processParameterInfo == null)
        {
            throw new InvalidOperationException($"Not found parameter of Consumer.Process for topic {topicType.Key.Value}");
        }

        var parameterType = processParameterInfo.ParameterType;

        var processMethod = GetProcessMethod(topicType.Value, parameterType);

        if (processMethod == null)
        {
            throw new EntryPointNotFoundException(
                $"No implementation of method {parameterType.Name} Process({parameterType.Name} entity)");
        }
        
        var entity = Deserialize(messagePayload, parameterType);

        return new ConsumerResolveResult()
        {
            Consumer = service,
            ConsumerType = topicType.Value,
            MessageType = parameterType,
            ProcessMethod = processMethod,
            DeserializedEntity = entity
        };
    }

    private MethodInfo? GetProcessMethod(Type? consumerType, Type? entityType)
    {
        if (consumerType == null)
        {
            throw new ArgumentNullException(nameof(consumerType));
        }

        if (entityType == null)
        {
            throw new ArgumentNullException(nameof(entityType));
        }

        return consumerType.GetMethods().Where(t => t.Name.Equals("Process")
                                                    && t.GetParameters().Length == 1 &&
                                                    t.GetParameters().FirstOrDefault()!.ParameterType
                                                        .Name.Equals(entityType.Name)
            ).Select(x => x)
            .FirstOrDefault();
    }

    private object? Deserialize(string? content, Type? type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return string.IsNullOrWhiteSpace(content)
            ? null
            : JsonSerializer.Deserialize(content, type, new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            });
    }
}