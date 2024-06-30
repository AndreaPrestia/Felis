using Felis.Router.Entities;
using LiteDB;

namespace Felis.Router.Services;

internal class QueueService : IDisposable
{
    private readonly ILiteDatabase _database;
    private readonly ILiteCollection<QueueEntity> _queueCollection;
    private readonly object _lock = new object();

    public QueueService(ILiteDatabase database)
    {
        _database = database;
        _queueCollection = _database.GetCollection<QueueEntity>("queues");
        _queueCollection.EnsureIndex(x => x.Timestamp);
    }

    public void Enqueue(Guid messageId)
    {
        if(messageId == Guid.Empty)
        {
            throw new ArgumentException(nameof(messageId));
        }
         
        lock (_lock)
        {
            var item = new QueueEntity
            {
                Id = messageId,
                Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()
            };

            _queueCollection.Insert(messageId, item);
        }
    }

    public QueueEntity? Dequeue()
    {
        lock (_lock)
        {
            var item = _queueCollection.FindOne(Query.All("Timestamp", Query.Ascending));
            if (item != null)
            {
                _queueCollection.Delete(item.Id);
            }
            return item;
        }
    }

    public void Dispose()
    {
        _database.Dispose();
    }
}