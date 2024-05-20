using Felis.Client.Attributes;
using Felis.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;

namespace Felis.Client.Resolvers;

internal sealed class ConsumerResolver
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ConsumerResolver(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    internal ConsumerResolveResult ResolveConsumerByTopic(KeyValuePair<string, Type> topicType, string? messagePayload)
    {
        try
        {
           return GetConsumer(topicType, messagePayload);
        }
        catch (Exception ex)
        {
            return ConsumerResolveResult.Ko(ex);
        }
    }
    
    internal Dictionary<string, Type> GetTypesForTopics()
    {
        var topicTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           type.GetInterfaces().Any(i =>
                               i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsume<>))).SelectMany(t =>
                t.GetCustomAttributes<TopicAttribute>()
                    .Select(x => new KeyValuePair<string, Type>(x.Value!, t)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return topicTypes;
    }
    
    private ConsumerResolveResult GetConsumer(KeyValuePair<string, Type> topicType, string? messagePayload)
    {
        if (topicType.Key == null)
        {
            throw new ArgumentNullException(nameof(topicType.Key));
        }
        
        if (topicType.Value == null)
        {
            throw new ArgumentNullException(nameof(topicType.Value));
        }
        
        var processParameterInfo = topicType.Value.GetMethod("Process")?.GetParameters().FirstOrDefault();

        if (processParameterInfo == null)
        {
            throw new InvalidOperationException($"Not found parameter of Consumer.Process for topic {topicType.Key}");
        }

        var parameterType = processParameterInfo.ParameterType;
        
        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        
        var closedGenericType = typeof(IConsume<>).MakeGenericType(parameterType);

        var services = provider.GetServices(closedGenericType).ToList();

        if (services == null || !services.Any())
        {
            throw new ApplicationException($"No consumers registered for topic {topicType.Key}");
        }

        var service = services.FirstOrDefault(e => e != null && e.GetType().FullName == topicType.Value.FullName);

        var processMethod = GetProcessMethod(topicType.Value, parameterType);

        if (processMethod == null)
        {
            throw new EntryPointNotFoundException(
                $"No implementation of method {parameterType.Name} Process({parameterType.Name} entity)");
        }
        
        var entity = Deserialize(messagePayload, parameterType);

        return ConsumerResolveResult.Ok(service, topicType.Value, parameterType, processMethod, entity);
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