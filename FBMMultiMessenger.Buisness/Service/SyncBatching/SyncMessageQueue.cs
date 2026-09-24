using System.Threading.Channels;
using FBMMultiMessenger.Buisness.Request.Chat;

namespace FBMMultiMessenger.Buisness.Service.SyncBatching
{
    /// <summary>One parsed "insertNewMessageRange" chunk queued for batched persistence.</summary>
    public record SyncBatchItem
    {
        public int UserId { get; init; }
        public int AccountId { get; init; }
        public string FbAccountId { get; init; } = string.Empty;
        public List<SyncChatsModel> Chats { get; init; } = new();
    }

    public interface ISyncMessageQueue
    {
        void Enqueue(SyncBatchItem item);
        ChannelReader<SyncBatchItem> Reader { get; }
    }

    /// <summary>
    /// In-memory queue of parsed history-sync chunks, drained and persisted in batches by
    /// <see cref="SyncFlushService"/> so a scroll burst doesn't open a DB connection per chunk.
    /// Bounded; dropping oldest is safe because it's old history re-synced on the next open.
    /// </summary>
    public class SyncMessageQueue : ISyncMessageQueue
    {
        private readonly Channel<SyncBatchItem> _channel =
            Channel.CreateBounded<SyncBatchItem>(new BoundedChannelOptions(50_000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            });

        public ChannelReader<SyncBatchItem> Reader => _channel.Reader;

        public void Enqueue(SyncBatchItem item) => _channel.Writer.TryWrite(item);
    }
}
