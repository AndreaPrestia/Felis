using Felis.Core.Models;
using Felis.Router.Abstractions;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace Felis.Router.Storage
{
    internal sealed class LiteDbRouterStorage : IRouterStorage
    {
        private readonly ILiteDatabase _database;
        private readonly ILogger<LiteDbRouterStorage> _logger;
        private readonly object _lock = new();

        public LiteDbRouterStorage(ILiteDatabase database, ILogger<LiteDbRouterStorage> logger)
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(logger);
            _database = database;
            _logger = logger;
        }

        public bool ConsumedMessageAdd(ConsumedMessage? consumedMessage)
        {
            ArgumentNullException.ThrowIfNull(consumedMessage);
            ArgumentNullException.ThrowIfNull(consumedMessage.Message);
            ArgumentNullException.ThrowIfNull(consumedMessage.Message.Header);
            ArgumentNullException.ThrowIfNull(consumedMessage.Message.Header.Topic);

            lock (_lock)
            {
                if(consumedMessage.Id == Guid.Empty)
                {
                    consumedMessage.Id = consumedMessage.Message.Header.Id;
                }

                var consumedCollection = _database.GetCollection<ConsumedMessage>("messages_consumed");

                var messageFound = consumedCollection.FindById(consumedMessage.Id);

                if (messageFound != null)
                {
                    _logger.LogWarning($"Message {consumedMessage.Message.Header.Id} already consumed");
                    return false;
                }

                consumedCollection.Insert(consumedMessage.Id, consumedMessage);

                var sentCollection = _database.GetCollection<Message?>("messages_sent");

                _logger.LogDebug($"Message {consumedMessage.Message.Header.Id} removing from sent");

                var deleteResult = sentCollection.Delete(consumedMessage.Message.Header.Id);

                _logger.LogDebug($"Message {consumedMessage.Message.Header.Id} removed from sent {deleteResult}");

                return deleteResult;
            }
        }

        public bool ReadyMessageAdd(Message? message)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(message.Header);
            ArgumentNullException.ThrowIfNull(message.Header.Topic);

            lock (_lock)
            {
                var readyCollection = _database.GetCollection<Message?>("messages_ready");

                readyCollection.Insert(message.Header.Id, message);

                return true;
            }
        }

        public Message? ReadyMessageGet()
        {
            lock (_lock)
            {
                var readyCollection = _database.GetCollection<Message?>("messages_ready");

                var messageFound = readyCollection.Query().Where(x => x != null && x.Header != null).OrderBy(e => e!.Header!.Timestamp).FirstOrDefault();

                if (messageFound == null)
                {
                    _logger.LogWarning("No ready message found to send");
                    return null;
                }

                readyCollection.Delete(messageFound.Header!.Id);

                return messageFound;
            }
        }

        public List<Message?> ReadyMessageList(Topic? topic = null)
        {
            lock (_lock)
            {
                var readyCollection = _database.GetCollection<Message?>("messages_ready");

                var messages = readyCollection.Query().Where(x => x != null &&
                    topic != null ? x.Header!.Topic!.Value == topic.Value : x != null && x.Header!.Id != Guid.Empty).ToList();

                return messages;
            }
        }

        public bool SentMessageAdd(Message? message)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(message.Header);
            ArgumentNullException.ThrowIfNull(message.Header.Topic);

            lock (_lock)
            {
                var readyCollection = _database.GetCollection<Message?>("messages_sent");

                readyCollection.Insert(message.Header.Id, message);

                return true;
            }
        }

        public List<Message?> SentMessageList(Topic? topic = null)
        {
            lock (_lock)
            {
                var sentCollection = _database.GetCollection<Message?>("messages_sent");

                var messages = sentCollection.Query().Where(x => x != null &&
                                                                 topic != null ? x.Header!.Topic!.Value == topic.Value : x != null && x.Header!.Id != Guid.Empty).ToList();

                return messages;
            }
        }

        public List<ConsumedMessage?> ConsumedMessageList(ConnectionId connectionId)
        {
            lock (_lock)
            {
                var consumedCollection = _database.GetCollection<ConsumedMessage?>("messages_consumed");

                var messages = consumedCollection.Query().Where(x => x != null && x.ConnectionId.Value == connectionId.Value).ToList();

                return messages;
            }
        }

        public List<ConsumedMessage?> ConsumedMessageList(Topic topic)
        {
            lock (_lock)
            {
                var consumedCollection = _database.GetCollection<ConsumedMessage?>("messages_consumed");

                var messages = consumedCollection.Query().Where(x => x != null && x.Message!.Header!.Topic!.Value == topic.Value).ToList();

                return messages;
            }
        }

        public List<ConsumedMessage?> ConsumedMessageList(ConnectionId connectionId, Topic topic)
        {
            lock (_lock)
            {
                var consumedCollection = _database.GetCollection<ConsumedMessage?>("messages_consumed");

                var messages = consumedCollection.Query().Where(x => x != null && x.ConnectionId.Value == connectionId.Value && x.Message!.Header!.Topic!.Value == topic.Value).ToList();

                return messages;
            }
        }

        public bool ReadyMessagePurge(Topic? topic)
        {
            ArgumentNullException.ThrowIfNull(topic);

            lock (_lock)
            {
                var readyMessageCollection = _database.GetCollection<Message?>("messages_ready");

                var deleteResult = readyMessageCollection.DeleteMany(m => m!.Header!.Topic!.Value == topic.Value);

                return deleteResult > 0;
            }
        }

        public bool ReadyMessagePurge(int? timeToLiveMinutes)
        {
            if (!timeToLiveMinutes.HasValue || timeToLiveMinutes <= 0)
            {
                return false;
            }

            lock (_lock)
            {
                var readyMessageCollection = _database.GetCollection<Message?>("messages_ready");

                var deleteResult = readyMessageCollection.DeleteMany(m => m!.Header!.Timestamp <
                                                                          new DateTimeOffset(DateTime.UtcNow)
                                                                              .AddMinutes(-timeToLiveMinutes.Value)
                                                                              .ToUnixTimeMilliseconds());

                return deleteResult > 0;
            }
        }

        public bool ErrorMessageAdd(ErrorMessage? message)
        {
            lock (_lock)
            {
                if (message?.Message?.Header == null)
                {
                    return false;
                }

                if (message.RetryPolicy is not { Attempts: > 0 })
                {
                    _logger.LogWarning($"Error message without Attempts: {System.Text.Json.JsonSerializer.Serialize(message)}");
                    return true;
                }

                var errorMessageWithRetriesDoneCollection = _database.GetCollection<ErrorMessageWithRetries?>("messages_error_with_retries");

                var retryFound = errorMessageWithRetriesDoneCollection.FindById(message.Message.Header.Id);

                var errorMessageCollection = _database.GetCollection<ErrorMessage?>("messages_error");

                if (retryFound == null)
                {
                    retryFound = new ErrorMessageWithRetries()
                    {
                        Id = message.Message.Header.Id,
                        RetryCount = 1
                    };

                    errorMessageCollection.Insert(message.Message.Header.Id, message);
                    errorMessageWithRetriesDoneCollection.Insert(message.Message.Header.Id, retryFound);

                    return true;
                }

                var errorMessageFound =
                    errorMessageCollection.Query().Where(x => x != null && x.Message != null && x.Message.Header != null && x.Message.Header.Id != message.Message.Header.Id).FirstOrDefault();

                if (errorMessageFound == null)
                {
                    errorMessageWithRetriesDoneCollection.Insert(message.Message.Header.Id, retryFound);
                }

                retryFound.RetryCount += 1;

                var updateResult = errorMessageWithRetriesDoneCollection.Update(retryFound.Id, retryFound);

                return updateResult;
            }
        }

        public ErrorMessage? ErrorMessageGet()
        {
            lock (_lock)
            {
                var errorCollection = _database.GetCollection<ErrorMessage?>("messages_error");

                var errorMessage = errorCollection.Query().Where(x => x != null && x.Message!.Header != null).OrderBy(e => e!.Message!.Header!.Timestamp).FirstOrDefault();

                if (errorMessage == null)
                {
                    _logger.LogWarning("No error message found to send");
                    return null;
                }

                var errorMessageWithRetriesDoneCollection = _database.GetCollection<ErrorMessageWithRetries?>("messages_error_with_retries");

                var retriesDone = errorMessageWithRetriesDoneCollection.Query().Where(em => em!.Id == errorMessage.Message!.Header!.Id && errorMessage.RetryPolicy!.Attempts >= em.RetryCount).FirstOrDefault()?.RetryCount;

                if (retriesDone > errorMessage.RetryPolicy?.Attempts)
                {
                    _logger.LogWarning($"Error message finished Attempts: {System.Text.Json.JsonSerializer.Serialize(errorMessage)}");
                    return null;
                }

                return retriesDone <= errorMessage.RetryPolicy?.Attempts ? null : errorMessage;
            }
        }

        public List<ErrorMessage?> ErrorMessageList(Topic? topic = null)
        {
            lock (_lock)
            {
                var errorCollection = _database.GetCollection<ErrorMessage?>("messages_error");

                var messages = errorCollection.Query().Where(x => x != null && x.Message != null && x.Message.Header != null && x.Message.Header.Topic != null &&
                                                                  topic != null ? x.Message.Header.Topic.Value == topic.Value : x != null && x.Message != null && x.Message.Header != null && x.Message.Header.Id != Guid.Empty).ToList();

                return messages;
            }
        }
    }

    public class ErrorMessageWithRetries
    {
        public Guid Id { get; set; }
        public int RetryCount { get; set; }
    }
}
