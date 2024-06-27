using Felis.Common.Models;
using Felis.Router.Entities;
using LiteDB;

namespace Felis.Router.Services;

internal sealed class DeadLetterService: IDisposable
{
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<DeadLetterEntity> _deadLetterCollection;
    private readonly object _lock = new object();

    public DeadLetterService(ILiteDatabase database)
    {
        _database = database;
        _deadLetterCollection = _database.GetCollection<DeadLetterEntity>("deadletters");
        _deadLetterCollection.EnsureIndex(x => x.Timestamp);
    }
    
    public void Add(Guid messageId, MessageSendStatus status)
    {
        if(messageId == Guid.Empty)
        {
            throw new ArgumentException(nameof(messageId));
        }
        
        lock (_lock)
        {
            var item = new DeadLetterEntity
            {
                Id = Guid.NewGuid(),
                MessageId = messageId,
                Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(),
                Status = status.ToString()
            };

            _deadLetterCollection.Insert(item.Id, item);
        }
    }

    public void Dispose()
    {
        _database.Dispose();
    }
}