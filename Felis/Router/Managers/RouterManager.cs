﻿using Felis.Common.Models;
using Felis.Router.Entities;
using Felis.Router.Hubs;
using Felis.Router.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Managers;

public sealed class RouterManager : IDisposable
{
    private readonly ILogger<RouterManager> _logger;
    private readonly ConnectionService _connectionService;
    private readonly MessageService _messageService;
    private readonly DeadLetterService _deadLetterService;
    private readonly IHubContext<RouterHub> _hubContext;

    internal RouterManager(ILogger<RouterManager> logger, MessageService messageService,
        ConnectionService connectionService,
        DeadLetterService deadLetterService,
        IHubContext<RouterHub> hubContext)
    {
        _logger = logger;
        _messageService = messageService;
        _connectionService = connectionService;
        _deadLetterService = deadLetterService;
        _hubContext = hubContext;

        _connectionService.NotifyNewConnectedConsumer += OnNewConnectedConsumerNotifyReceived;
    }

    public async Task<MessageStatus> DispatchAsync(MessageRequest? message,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.Topic))
        {
            throw new ArgumentNullException($"No Topic provided");
        }

        var sendMessageResponse = await SendMessageAsync(message.Id, null, cancellationToken);

        var result = sendMessageResponse.MessageSendStatus == MessageSendStatus.MessageSent
            ? MessageStatus.Sent
            : sendMessageResponse.MessageSendStatus == MessageSendStatus.MessageReady
                ? MessageStatus.Ready
                : MessageStatus.Error;

        if (result == MessageStatus.Error)
        {
            _logger.LogWarning("Cannot add message in storage.");
        }

        return result;
    }

    public MessageStatus Consume(ConsumedMessage? consumedMessage)
    {
        if (consumedMessage == null)
        {
            throw new ArgumentNullException(nameof(consumedMessage));
        }

        var result = _messageService.Consume(consumedMessage);

        if (result == MessageStatus.Error)
        {
            _logger.LogWarning("Cannot add consumed message in storage.");
        }

        return result;
    }

    public async Task<MessageStatus> ErrorAsync(ErrorMessageRequest? errorMessage,
        CancellationToken cancellationToken = default)
    {
        if (errorMessage == null)
        {
            throw new ArgumentNullException(nameof(errorMessage));
        }

        var result = _messageService.Error(errorMessage);

        if (result == MessageStatus.Error)
        {
            _logger.LogWarning("Cannot add error message in storage.");
            return result;
        }

        if (result != MessageStatus.Ready)
        {
            _logger.LogWarning($"Message {errorMessage.Id} status {result}. No processing will be done.");
            return result;
        }
        
        var sendMessageResponse =
            await SendMessageAsync(errorMessage.Id, errorMessage.ConnectionId, cancellationToken);
        result = sendMessageResponse.MessageSendStatus == MessageSendStatus.MessageSent
            ? MessageStatus.Sent
            : sendMessageResponse.MessageSendStatus == MessageSendStatus.MessageReady
                ? MessageStatus.Ready
                : MessageStatus.Error;
        
        _logger.LogDebug($"Re-enqueued message {errorMessage.Id}");

        return result;
    }

    public MessageStatus Process(ProcessedMessage? processedMessage)
    {
        if (processedMessage == null)
        {
            throw new ArgumentNullException(nameof(processedMessage));
        }

        var result = _messageService.Process(processedMessage);

        if (result == MessageStatus.Error)
        {
            _logger.LogWarning("Cannot add processed message in storage.");
        }

        return result;
    }

    public int Purge(string topic) => _messageService.Purge(topic);

    public List<Consumer> Consumers(string topic) => _connectionService.GetConnectedConsumers(topic);

    public List<Message> ReadyList(string? topic = null) => _messageService.ReadyList(topic);

    public List<Message> SentList(string? topic = null) => _messageService.SentList(topic);

    public List<ErrorMessage> ErrorList(string? topic = null) => _messageService.ErrorList(topic);

    public List<ConsumedMessage> ConsumedMessageList(string topic) => _messageService.ConsumedMessageList(topic);

    public List<ConsumedMessage> ConsumedListByConnectionId(string connectionId) =>
        _messageService.ConsumedListByConnectionId(connectionId);

    public List<ConsumedMessage> ConsumedList(string connectionId, string topic) =>
        _messageService.ConsumedList(connectionId, topic);

    private async void OnNewConnectedConsumerNotifyReceived(object sender, NewConsumerConnectedEventArgs e)
    {
        foreach (var message in e.Consumer.Topics.Select(topic => _messageService.ReadyList(topic)).Where(messages => messages.Any()).SelectMany(messages => messages))
        {
            var result = await SendMessageAsync(message.Header!.Id, e.ConnectionId);
            _logger.LogInformation($"Send ready message {message.Header!.Id} to new connection {e.ConnectionId} with result {result}");
        }
    }
    
    private async Task<NextMessageSentResponse> SendMessageAsync(Guid messageId, string? connectionId,
        CancellationToken cancellationToken = default)
    {
        var message = _messageService.Get(messageId);

        if (message == null)
        {
            _deadLetterService.Add(messageId, MessageSendStatus.MessageNotFound);
            _logger.LogWarning($"Cannot find message {messageId} in messages. No processing will be done.");
            return new NextMessageSentResponse(messageId, MessageSendStatus.MessageNotFound);
        }

        if (message.Status != MessageStatus.Ready.ToString())
        {
            _deadLetterService.Add(messageId, MessageSendStatus.MessageNotReady);
            _logger.LogWarning($"Message {messageId} has status {message.Status}. No processing will be done.");
            return new NextMessageSentResponse(messageId, MessageSendStatus.MessageNotReady);
        }

        var topic = message.Header?.Topic;

        if (string.IsNullOrWhiteSpace(topic))
        {
            _deadLetterService.Add(messageId, MessageSendStatus.MessageWithoutTopic);
            _logger.LogWarning($"No topic for {messageId} in messages. No processing will be done.");
            return new NextMessageSentResponse(messageId, MessageSendStatus.MessageWithoutTopic);
        }

        var consumerConnectionEntities = new List<ConsumerConnectionEntity>();

        if (!string.IsNullOrWhiteSpace(connectionId))
        {
            var consumerConnectionEntity = _connectionService.GetConsumerByConnectionId(connectionId);

            if (consumerConnectionEntity == null)
            {
                _deadLetterService.Add(messageId, MessageSendStatus.ConnectionIdNotFound);
                _logger.LogWarning(
                    $"No connection id {connectionId} for {messageId} in messages. No processing will be done.");
                return new NextMessageSentResponse(messageId, MessageSendStatus.ConnectionIdNotFound);
            }

            consumerConnectionEntities.Add(consumerConnectionEntity);
        }
        else
        {
            consumerConnectionEntities = _connectionService.GetConnectionIds(topic);
        }

        if (!consumerConnectionEntities.Any())
        {
            _logger.LogWarning(
                $"No connection ids found for topic {topic}. The message {messageId} remains in ready status.");
            return new NextMessageSentResponse(messageId, MessageSendStatus.MessageReady);
        }

        _logger.LogInformation($"Sending message {messageId} for topic {topic}");

        var uniqueConsumer = consumerConnectionEntities.Where(x => x.Consumer.Unique).MinBy(x => x.Timestamp);

        if (uniqueConsumer != null)
        {
            _logger.LogInformation($"Found unique consumer {uniqueConsumer.ConnectionId} for topic {topic}");
            consumerConnectionEntities = new List<ConsumerConnectionEntity>()
            {
                uniqueConsumer
            };
        }

        await _hubContext.Clients.Clients(consumerConnectionEntities.Select(x => x.ConnectionId).ToList())
            .SendAsync(topic, message, cancellationToken);

        var messageStatus = _messageService.Send(messageId);

        _logger.LogWarning($"Message {message.Header?.Id} sent {messageStatus}.");

        if (messageStatus != MessageStatus.Sent)
        {
            _deadLetterService.Add(messageId, MessageSendStatus.MessageNotSent);
        }

        return messageStatus == MessageStatus.Sent
            ? new NextMessageSentResponse(messageId, MessageSendStatus.MessageSent)
            : new NextMessageSentResponse(messageId, MessageSendStatus.MessageNotSent);
    }

    public void Dispose()
    {
        _connectionService.NotifyNewConnectedConsumer -= OnNewConnectedConsumerNotifyReceived;
        _messageService.Dispose();
        _deadLetterService.Dispose();
    }
}