using Felis.Router.Entities;
using LiteDB;

namespace Felis.Router.Services
{
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
                    MessageId = messageId,
                    Timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()
                };

                _queueCollection.Insert(item);
            }
        }

        public QueueEntity? Dequeue()
        {
            lock (_lock)
            {
                var item = _queueCollection.FindOne(Query.All("Timestamp", Query.Ascending));
                if (item != null)
                {
                    _queueCollection.Delete(item.MessageId);
                }
                return item;
            }
        }

        public QueueEntity? Peek()
        {
            lock (_lock)
            {
                return _queueCollection.FindOne(Query.All("Timestamp", Query.Ascending));
            }
        }

        public int Count()
        {
            lock (_lock)
            {
                return _queueCollection.Count();
            }
        }

        public void Dispose()
        {
            _database.Dispose();
        }
    }
}
