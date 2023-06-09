﻿using System.Collections.Concurrent;
using Felis.Core;
using Felis.Core.Models;
using Felis.Router.Interfaces;

namespace Felis.Router.Storage;

/// <summary>
/// This is an in-memory storage implementation of IFelisRouterStorage. Just for academic purposes. We must use it at most as cache :) 
/// </summary>
public class FelisRouterStorage : IFelisRouterStorage
{
    private ConcurrentQueue<Message?> _messages = new();
    private ConcurrentQueue<ConsumedMessage?> _consumedMessages = new();
    private ConcurrentQueue<ErrorMessage?> _errorMessages = new();

    public bool ConsumedMessageAdd(ConsumedMessage? consumedMessage)
    {
        if (_consumedMessages.Any(cm => cm?.Service == consumedMessage?.Service
                                        && string.Equals(cm?.Message?.Header?.Topic?.Value,
                                            consumedMessage?.Message?.Header?.Topic?.Value,
                                            StringComparison.InvariantCultureIgnoreCase)
                                        && cm?.Timestamp == consumedMessage?.Timestamp))
        {
            return false;
        }

        _consumedMessages = new ConcurrentQueue<ConsumedMessage?>(_consumedMessages.Append(consumedMessage));

        return true;
    }

    public bool MessageAdd(Message? message)
    {
        if (_messages.Any(m =>
                string.Equals(m?.Header?.Topic?.Value, message?.Header?.Topic?.Value,
                    StringComparison.InvariantCultureIgnoreCase)
                && m?.Header?.Timestamp == message?.Header?.Timestamp))
        {
            return false;
        }

        _messages = new ConcurrentQueue<Message?>(_messages.Append(message));

        return true;
    }

    public List<Message?> MessageList(Topic? topic = null)
    {
        return _messages.Where(m =>
                topic == null || string.IsNullOrWhiteSpace(topic.Value) ||
                m!.Header!.Topic!.Value!.Contains(topic.Value))
            .ToList();
    }

    public List<ConsumedMessage?> ConsumedMessageList(Service service)
    {
        return _consumedMessages.Where(cm => cm?.Service?.Host == service.Host && cm?.Service?.Name == service.Name)
            .ToList();
    }

    public List<ConsumedMessage?> ConsumedMessageList(Topic topic)
    {
        return _consumedMessages
            .Where(cm =>
                string.Equals(cm?.Message?.Header?.Topic?.Value, topic.Value,
                    StringComparison.InvariantCultureIgnoreCase))
            .ToList();
    }

    public bool MessagePurge(Topic? topic)
    {
        _messages = new ConcurrentQueue<Message?>(_messages.Where(m =>
                !string.Equals(topic?.Value, m?.Header?.Topic?.Value, StringComparison.InvariantCultureIgnoreCase))
            .OrderBy(m => m?.Header?.Timestamp));

        return true;
    }

    public bool MessagePurge(int? timeToLiveMinutes)
    {
        if (!timeToLiveMinutes.HasValue || timeToLiveMinutes <= 0)
        {
            return false;
        }

        _messages = new ConcurrentQueue<Message?>(_messages
            .Where(m => m?.Header?.Timestamp < new DateTimeOffset(DateTime.UtcNow).AddMinutes(-timeToLiveMinutes.Value)
                .ToUnixTimeMilliseconds())
            .OrderBy(m => m?.Header?.Timestamp));

        return true;
    }

    public bool ErrorMessageAdd(ErrorMessage? message)
    {
        _errorMessages = new ConcurrentQueue<ErrorMessage?>(_errorMessages.Append(message));

        return true;
    }

    public List<ErrorMessage?> ErrorMessageList(Topic? topic = null, long? start = null, long? end = null)
    {
        return _errorMessages
            .Where(em =>
                (topic == null || string.Equals(em?.Message?.Header?.Topic?.Value, topic.Value,
                    StringComparison.InvariantCultureIgnoreCase))
                && ((start == null && end == null) || (em?.Timestamp >= start && em?.Timestamp <= end))).ToList();
    }
}