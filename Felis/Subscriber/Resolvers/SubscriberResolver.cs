using System.Reflection;
using System.Text.Json;
using Felis.Common.Models;
using Felis.Subscriber.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Felis.Subscriber.Resolvers;

internal sealed class SubscriberResolver
{
      private readonly IServiceScopeFactory _scopeFactory;

    public SubscriberResolver(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    internal ConsumerResolveResult ResolveSubscriberByTopic(KeyValuePair<TopicValue, Type> topicType, string? messagePayload)
    {
        try
        {
           return GetSubscriber(topicType, messagePayload);
        }
        catch (Exception ex)
        {
            return ConsumerResolveResult.Ko(ex);
        }
    }
    
    internal Dictionary<TopicValue, Type> GetTypesForTopics()
    {
        var topicTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           type.GetInterfaces().Any(i =>
                               i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISubscribe<>))).SelectMany(t =>
                t.GetCustomAttributes<TopicAttribute>()
                    .Select(x => new KeyValuePair<TopicValue, Type>(new TopicValue(x.Value!), t)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return topicTypes;
    }
    
    private ConsumerResolveResult GetSubscriber(KeyValuePair<TopicValue, Type> topicType, string? messagePayload)
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
            throw new InvalidOperationException($"Not found parameter of ISubscribe.Process for topic {topicType.Key}");
        }

        var parameterType = processParameterInfo.ParameterType;
        
        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        
        var closedGenericType = typeof(ISubscribe<>).MakeGenericType(parameterType);

        var services = provider.GetServices(closedGenericType).ToList();

        if (services == null || !services.Any())
        {
            throw new ApplicationException($"No consumers registered for topic {topicType.Key}");
        }

        var service = services.FirstOrDefault(e => e != null && e.GetType().FullName == topicType.Value.FullName);

        var listenMethod = GetListenMethod(topicType.Value, parameterType);

        if (listenMethod == null)
        {
            throw new EntryPointNotFoundException(
                $"No implementation of method {parameterType.Name} Listen({parameterType.Name} entity)");
        }
        
        var entity = Deserialize(messagePayload, parameterType);

        return ConsumerResolveResult.Ok(service, topicType.Value, parameterType, listenMethod, entity);
    }

    private MethodInfo? GetListenMethod(Type? consumerType, Type? entityType)
    {
        if (consumerType == null)
        {
            throw new ArgumentNullException(nameof(consumerType));
        }

        if (entityType == null)
        {
            throw new ArgumentNullException(nameof(entityType));
        }

        return consumerType.GetMethods().Where(t => t.Name.Equals("Listen")
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