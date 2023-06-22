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
    private ConcurrentQueue<Message> _messages = new ConcurrentQueue<Message>();
    private ConcurrentQueue<ConsumedMessage> _consumedMessages = new ConcurrentQueue<ConsumedMessage>();

    public void ConsumedMessageAdd(ConsumedMessage consumedMessage)
    {
        if (_consumedMessages.Any(cm => cm.Client == consumedMessage.Client
                                        && string.Equals(cm?.Message?.Topic?.Value, consumedMessage?.Message?.Topic?.Value,
                                            StringComparison.InvariantCultureIgnoreCase)
                                        && cm?.Timestamp == consumedMessage?.Timestamp))
        {
            return;
        }

        _consumedMessages = new ConcurrentQueue<ConsumedMessage>(_consumedMessages.Append(consumedMessage));
    }

    public void MessageAdd(Message message)
    {
        if (_messages.Any(m => string.Equals(m?.Topic?.Value, message?.Topic?.Value, StringComparison.InvariantCultureIgnoreCase)
                               && m?.Timestamp == message?.Timestamp))
        {
            return;
        }

        _messages = new ConcurrentQueue<Message>(_messages.Append(message));
    }

    public List<Message> MessageList(string? topic = null)
    {
        return _messages.Where(m => string.IsNullOrWhiteSpace(topic) || m.Topic!.Value!.Contains(topic)).ToList();
    }

    public List<ConsumedMessage> ConsumedMessageList(Client client)
    {
        return _consumedMessages.Where(cm => cm.Client?.Value == client.Value).ToList();
    }

    public List<ConsumedMessage> ConsumedMessageList(Topic topic)
    {
        return _consumedMessages
            .Where(cm => string.Equals(cm.Message?.Topic?.Value, topic.Value, StringComparison.InvariantCultureIgnoreCase)).ToList();
    }

    public void MessagePurge(string topic)
    {
        _messages = new ConcurrentQueue<Message>(_messages.Where(m =>
            !string.Equals(topic, m.Topic?.Value, StringComparison.InvariantCultureIgnoreCase)).OrderBy(m => m.Timestamp));
    }
}