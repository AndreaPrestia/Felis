using Felis.Router.Enums;
using Felis.Router.Hubs;
using Felis.Router.Models;
using Felis.Router.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Managers;

public sealed class RouterManager
{
    private readonly ILogger<RouterManager> _logger;
    private readonly ConnectionService _connectionService;
    private readonly MessageService _messageService;
    private readonly QueueService _queueService;
    private readonly LoadBalancingService _loadBalancingService;
    private readonly DeadLetterService _deadLetterService;
    private readonly IHubContext<RouterHub> _hubContext;

    internal RouterManager(ILogger<RouterManager> logger, MessageService messageService,
        ConnectionService connectionService, QueueService queueService, LoadBalancingService loadBalancingService,
        DeadLetterService deadLetterService,
        IHubContext<RouterHub> hubContext)
    {
        _logger = logger;
        _messageService = messageService;
        _connectionService = connectionService;
        _queueService = queueService;
        _loadBalancingService = loadBalancingService;
        _deadLetterService = deadLetterService;
        _hubContext = hubContext;
    }

    public MessageStatus Dispatch(string topic, MessageRequest? message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentNullException($"No Topic provided");
        }

        if (string.IsNullOrWhiteSpace(message.Topic))
        {
            throw new ArgumentNullException($"No Topic provided");
        }

        if (!string.Equals(message.Topic, topic, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new InvalidOperationException("The topic provided in message and route are not matching");
        }

        var result = _messageService.Add(message);

        if (result == MessageStatus.Error)
        {
            _logger.LogWarning("Cannot add message in storage.");
            return result;
        }

        _queueService.Enqueue(message.Id);

        return result;
    }

    public MessageStatus Consume(Guid id, ConsumedMessage? consumedMessage)
    {
        if (consumedMessage == null)
        {
            throw new ArgumentNullException(nameof(consumedMessage));
        }

        if (consumedMessage.Id != id)
        {
            throw new InvalidOperationException("The id provided in message and route are not matching");
        }

        if (consumedMessage.Id != id)
        {
            throw new InvalidOperationException("The id provided in message and route are not matching");
        }

        var result = _messageService.Consume(consumedMessage);

        if (result == MessageStatus.Error)
        {
            _logger.LogWarning("Cannot add consumed message in storage.");
        }

        return result;
    }

    public MessageStatus Error(Guid id, ErrorMessageRequest? errorMessage)
    {
        if (errorMessage == null)
        {
            throw new ArgumentNullException(nameof(errorMessage));
        }

        if (errorMessage.Id != id)
        {
            throw new InvalidOperationException("The id provided in message and route are not matching");
        }

        var result = _messageService.Error(errorMessage);

        switch (result)
        {
            case MessageStatus.Error:
                _logger.LogWarning("Cannot add error message in storage.");
                break;
            case MessageStatus.Ready:
                _queueService.Enqueue(id);
                _logger.LogDebug($"Re-enqueued message {id}");
                break;
        }

        return result;
    }

    public MessageStatus Process(Guid id, ProcessedMessage? processedMessage)
    {
        if (processedMessage == null)
        {
            throw new ArgumentNullException(nameof(processedMessage));
        }

        if (processedMessage.Id != id)
        {
            throw new InvalidOperationException("The id provided in message and route are not matching");
        }

        if (processedMessage.Id != id)
        {
            throw new InvalidOperationException("The id provided in message and route are not matching");
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

    public async Task<NextMessageSentResponse> SendNextMessageAsync(CancellationToken cancellationToken = default)
    {
        var queueItem = _queueService.Dequeue();

        if (queueItem == null || queueItem.Id == Guid.Empty)
            return new NextMessageSentResponse(Guid.Empty, MessageSendStatus.NothingToSend);

        var messageId = queueItem.Id;

        var message = _messageService.Get(messageId);

        if (message == null)
        {
            _deadLetterService.Add(messageId, MessageSendStatus.MessageNotFound);
            _logger.LogWarning($"Cannot find message {messageId} in messages. No processing will be done.");
            return new NextMessageSentResponse(Guid.Empty, MessageSendStatus.MessageNotFound);
        }

        if (message.Status != MessageStatus.Ready.ToString())
        {
            _deadLetterService.Add(messageId, MessageSendStatus.MessageNotReady);
            _logger.LogWarning($"Message {messageId} has status {message.Status}. No processing will be done.");
            return new NextMessageSentResponse(Guid.Empty, MessageSendStatus.MessageNotReady);
        }

        var topic = message.Header?.Topic;

        if (string.IsNullOrWhiteSpace(topic))
        {
            _deadLetterService.Add(messageId, MessageSendStatus.MessageWithoutTopic);
            _logger.LogWarning($"No topic for {messageId} in messages. No processing will be done.");
            return new NextMessageSentResponse(Guid.Empty, MessageSendStatus.MessageWithoutTopic);
        }

        _logger.LogInformation($"Sending message {messageId} for topic {topic}");

        var connectionId = _loadBalancingService.GetNextConnectionId(topic);

        if (connectionId == null || string.IsNullOrWhiteSpace(connectionId))
        {
            _logger.LogWarning($"No connectionId available for topic {topic}. Message {messageId} will be requeued.");
            _queueService.Enqueue(messageId);
            return new NextMessageSentResponse(Guid.Empty, MessageSendStatus.NoConnectionIdAvailableForTopic);
        }

        await _hubContext.Clients.Client(connectionId).SendAsync(topic, message, cancellationToken);

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
}