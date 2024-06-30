﻿using System.Reflection;
using System.Text.Json;
using Felis.Common.Models;
using Felis.Subscriber.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Felis.Subscriber.Resolvers;

internal sealed class ConsumerResolver
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ConsumerResolver(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    internal ConsumerResolveResult ResolveConsumerByQueue(KeyValuePair<QueueValue, Type> queueType, string? messagePayload)
    {
        try
        {
           return GetConsumer(queueType, messagePayload);
        }
        catch (Exception ex)
        {
            return ConsumerResolveResult.Ko(ex);
        }
    }
    
    internal Dictionary<QueueValue, Type> GetTypesForQueues()
    {
        var topicTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           type.GetInterfaces().Any(i =>
                               i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsume<>))).SelectMany(t =>
                t.GetCustomAttributes<QueueAttribute>()
                    .Select(x => new KeyValuePair<QueueValue, Type>(new QueueValue(x.Value!, x.Unique, x.RetryPolicy), t)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return topicTypes;
    }
    
    private ConsumerResolveResult GetConsumer(KeyValuePair<QueueValue, Type> queueType, string? messagePayload)
    {
        if (queueType.Key == null)
        {
            throw new ArgumentNullException(nameof(queueType.Key));
        }
        
        if (queueType.Value == null)
        {
            throw new ArgumentNullException(nameof(queueType.Value));
        }
        
        var processParameterInfo = queueType.Value.GetMethod("Process")?.GetParameters().FirstOrDefault();

        if (processParameterInfo == null)
        {
            throw new InvalidOperationException($"Not found parameter of IConsume.Process for queue {queueType.Key}");
        }

        var parameterType = processParameterInfo.ParameterType;
        
        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        
        var closedGenericType = typeof(IConsume<>).MakeGenericType(parameterType);

        var services = provider.GetServices(closedGenericType).ToList();

        if (services == null || !services.Any())
        {
            throw new ApplicationException($"No consumers registered for queue {queueType.Key}");
        }

        var service = services.FirstOrDefault(e => e != null && e.GetType().FullName == queueType.Value.FullName);

        var processMethod = GetProcessMethod(queueType.Value, parameterType);

        if (processMethod == null)
        {
            throw new EntryPointNotFoundException(
                $"No implementation of method {parameterType.Name} Process({parameterType.Name} entity)");
        }
        
        var entity = Deserialize(messagePayload, parameterType);

        return ConsumerResolveResult.Ok(service, queueType.Value, parameterType, processMethod, entity);
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