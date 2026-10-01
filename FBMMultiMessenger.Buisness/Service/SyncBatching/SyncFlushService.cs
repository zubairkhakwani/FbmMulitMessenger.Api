using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sentry;

namespace FBMMultiMessenger.Buisness.Service.SyncBatching
{
    /// <summary>
    /// Drains queued history-sync chunks and persists them in batches, grouped per account, so a scroll
    /// burst uses one pooled DB connection at a time (per account) instead of one per chunk. Grouping and
    /// serial per-account processing also removes the row-contention that caused the DbUpdateException retries.
    /// </summary>
    public class SyncFlushService : BackgroundService
    {
        private readonly ISyncMessageQueue _queue;
        private readonly IServiceProvider _serviceProvider;

        private const int FlushWindowMs = 1000;
        private const int MaxBatchSize = 500;

        public SyncFlushService(ISyncMessageQueue queue, IServiceProvider serviceProvider)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reader = _queue.Reader;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!await reader.WaitToReadAsync(stoppingToken))
                    {
                        break;
                    }

                    var batch = await CollectBatchAsync(reader, stoppingToken);

                    if (batch.Count > 0)
                    {
                        await FlushAsync(batch, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                }
            }
        }

        private static async Task<List<SyncBatchItem>> CollectBatchAsync(ChannelReader<SyncBatchItem> reader, CancellationToken stoppingToken)
        {
            var batch = new List<SyncBatchItem>();
            var deadline = DateTime.UtcNow.AddMilliseconds(FlushWindowMs);

            while (batch.Count < MaxBatchSize)
            {
                if (reader.TryRead(out var item))
                {
                    batch.Add(item);
                    continue;
                }

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                using var windowCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                windowCts.CancelAfter(remaining);

                try
                {
                    if (!await reader.WaitToReadAsync(windowCts.Token))
                    {
                        break;
                    }
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    break; // window elapsed
                }
            }

            return batch;
        }

        private async Task FlushAsync(List<SyncBatchItem> batch, CancellationToken cancellationToken)
        {
            // Group all chunks by account and merge their chats, so an account's whole burst is one
            // DB transaction (the processor dedupes chats/messages internally).
            var groups = batch.GroupBy(x => (x.UserId, x.AccountId));

            foreach (var group in groups)
            {
                var (userId, accountId) = group.Key;
                var fbAccountId = group.First().FbAccountId;
                var mergedChats = group.SelectMany(g => g.Chats).ToList();

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<SyncMessagesProcessor>();
                    await processor.ProcessAsync(userId, accountId, fbAccountId, mergedChats, cancellationToken);
                }
                catch (Exception ex)
                {
                    // One account's failure must not stop the rest of the batch.
                    SentrySdk.CaptureException(ex, scope => scope.SetTag("accountId", accountId.ToString()));
                }
            }
        }
    }
}
