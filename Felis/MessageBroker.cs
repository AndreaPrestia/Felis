using System.Collections.Concurrent;
using Felis.Entities;
using Felis.Models;
using Felis.Services;
using Microsoft.Extensions.Logging;

namespace Felis;

internal sealed class MessageBroker : IDisposable
{
    private readonly ConcurrentDictionary<string, List<SubscriberEntity>> _topicSubscribers = new();
    private readonly ILogger<MessageBroker> _logger;
    private readonly MessageService _messageService;

    public MessageBroker(ILogger<MessageBroker> logger, MessageService messageService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(messageService);
        _logger = logger;
        _messageService = messageService;
    }

    /// <summary>
    /// Add a subscriber to a topic
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <param name="hostname"></param>
    /// <param name="topics"></param>
    /// <returns></returns>
    public SubscriberEntity Subscribe(string ipAddress, string hostname, List<string> topics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        ArgumentNullException.ThrowIfNull(topics);

        var subscriberEntity = new SubscriberEntity(ipAddress, hostname, topics);

        foreach (var topic in topics)
        {
            var subscribers = _topicSubscribers.GetOrAdd(topic.Trim(), _ => new List<SubscriberEntity>());
            lock (subscribers)
            {
                subscribers.Add(subscriberEntity);
            }

            var messages = _messageService.GetPendingMessagesToByTopic(topic);
            // Send pending messages when a subscriber connects
            foreach (var message in messages)
            {
                var writtenMessage = subscriberEntity.MessageChannel.Writer.TryWrite(message);
                _logger.LogDebug($"Written message {message.Id}: {writtenMessage}");
            }
        }

        return subscriberEntity;
    }

    /// <summary>
    /// Removes a subscriber by id
    /// </summary>
    /// <param name="id"></param>
    public void UnSubscribe(Guid id)
    {
        foreach (var topic in _topicSubscribers.Keys)
        {
            var subscribers = _topicSubscribers[topic];
            lock (subscribers)
            {
                subscribers.RemoveAll(s => s.Id == id);
            }
        }
    }

    /// <summary>
    /// Publish a message to a topic
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public void Publish(MessageModel message)
    {
        _messageService.Add(message);

        if (!_topicSubscribers.TryGetValue(message.Topic, out var subscribers)) return;
        if (subscribers.Count <= 0) return;
        
        foreach (var subscriber in subscribers)
        {
            var writtenMessage = subscriber.MessageChannel.Writer.TryWrite(message);
            _logger.LogDebug($"Written message {message.Id}: {writtenMessage}");
        }
    }

    /// <summary>
    /// Gets a list of subscribers by topic
    /// </summary>
    /// <param name="topic"></param>
    /// <returns></returns>
    public List<SubscriberModel> Subscribers(string topic)
    {
        try
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(topic);

            return _topicSubscribers.Where(x => x.Key.Contains(topic)).SelectMany(e => e.Value)
                .Select(r => new SubscriberModel(r.Hostname, r.IpAddress, r.Topics)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new List<SubscriberModel>();
        }
    }

    /// <summary>
    /// Sets a message in sent status
    /// </summary>
    /// <param name="messageId"></param>
    public void Send(Guid messageId) => _messageService.Send(messageId);

    public void Dispose()
    {
        _messageService.Dispose();
    }
}