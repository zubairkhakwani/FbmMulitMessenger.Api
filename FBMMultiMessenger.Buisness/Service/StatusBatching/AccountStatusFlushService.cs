using FBMMultiMessenger.Buisness.Models.SignalR.App;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Extensions;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Sentry;
using System.Text.Json;

namespace FBMMultiMessenger.Buisness.Service.StatusBatching
{
    /// <summary>
    /// Drains the account status queue and applies changes to the database in batches, so a burst of
    /// connects/disconnects/auth-updates costs one pooled DB connection at a controlled cadence instead
    /// of one connection per event. Changes are coalesced per account, per field (latest non-null wins).
    /// </summary>
    public class AccountStatusFlushService : BackgroundService
    {
        private readonly IAccountStatusQueue _queue;
        private readonly IServiceProvider _serviceProvider;

        private const int FlushWindowMs = 500;
        private const int MaxBatchSize = 500;

        // Consecutive failed flushes — drives the retry backoff and throttles failure diagnostics. Only the
        // single flush loop touches it, so no synchronization is needed.
        private int _consecutiveFailures;

        public AccountStatusFlushService(IAccountStatusQueue queue, IServiceProvider serviceProvider)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reader = _queue.Reader;

            while (!stoppingToken.IsCancellationRequested)
            {
                List<AccountStatusChange>? batch = null;

                try
                {
                    // Block until at least one change is available.
                    if (!await reader.WaitToReadAsync(stoppingToken))
                    {
                        break;
                    }

                    batch = await CollectBatchAsync(reader, stoppingToken);

                    if (batch.Count > 0)
                    {
                        await FlushAsync(batch, stoppingToken);
                    }

                    _consecutiveFailures = 0; // a clean flush clears the backoff/streak
                }
                catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
                {
                    SentrySdk.CaptureException(ex, scope =>
                    {
                        scope.SetTag("applicationStopped", "applicationStopped");
                    });
                    break;
                }
                catch (Exception ex)
                {
                    // Never let a bad flush kill the loop. On a transient DB failure the batch is re-queued
                    // (so status changes aren't lost — that's what leaves IsExtensionConnected stale); a
                    // permanent failure is logged and dropped.
                    await HandleFlushFailureAsync(ex, batch, stoppingToken);
                }
            }
        }

        private async Task HandleFlushFailureAsync(Exception ex, List<AccountStatusChange>? batch, CancellationToken stoppingToken)
        {
            _consecutiveFailures++;
            var transient = IsTransient(ex);

            // Re-queue on a transient failure so a dropped DB connection (e.g. 57P01) doesn't silently lose
            // the batch. A permanent failure (e.g. a constraint violation) is NOT re-queued — retrying it
            // forever would hot-loop. The queue is bounded/DropOldest, so re-queueing can't block or grow
            // without bound.
            if (transient && batch is { Count: > 0 })
            {
                foreach (var change in batch)
                {
                    _queue.Enqueue(change);
                }
            }

            // Throttle diagnostics so a sustained outage doesn't flood Sentry / the Logs folder: record the
            // first failure of a streak, then every 20th.
            if (_consecutiveFailures == 1 || _consecutiveFailures % 20 == 0)
            {
                var dataFile = WriteConflictDataToFile(batch ?? new List<AccountStatusChange>(), ex);
                SentrySdk.CaptureException(ex, scope =>
                {
                    scope.SetTag("dataFile", dataFile);
                    scope.SetTag("transient", transient.ToString());
                    scope.SetTag("flushConsecutiveFailures", _consecutiveFailures.ToString());
                });
            }

            // Back off before the next attempt so we don't hot-spin while the DB is unavailable (capped 30s).
            var delayMs = Math.Min(30_000, 500 * (int)Math.Pow(2, Math.Min(_consecutiveFailures, 6)));
            try
            {
                await Task.Delay(delayMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
        }

        // Transient = worth retrying (connection dropped, server restarting, too many connections, timeout).
        private static bool IsTransient(Exception ex)
        {
            for (Exception? e = ex; e != null; e = e.InnerException)
            {
                switch (e)
                {
                    case TimeoutException:
                        return true;
                    case PostgresException pg when IsTransientSqlState(pg.SqlState):
                        return true;
                    case NpgsqlException npg when npg.IsTransient:
                        return true;
                }
            }

            return false;
        }

        private static bool IsTransientSqlState(string? sqlState) => sqlState switch
        {
            "57P01" or "57P02" or "57P03"                         // admin shutdown / crash shutdown / cannot connect now
                or "53300"                                        // too many connections
                or "08000" or "08001" or "08003" or "08004" or "08006" // connection exceptions
                => true,
            _ => false
        };

        // Collect up to MaxBatchSize items, or until FlushWindowMs elapses — whichever comes first.
        private static async Task<List<AccountStatusChange>> CollectBatchAsync(
            System.Threading.Channels.ChannelReader<AccountStatusChange> reader, CancellationToken stoppingToken)
        {
            var batch = new List<AccountStatusChange>();
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
                catch (OperationCanceledException ex) when (!stoppingToken.IsCancellationRequested)
                {
                    break; // flush window elapsed
                }
            }

            return batch;
        }

        private async Task FlushAsync(List<AccountStatusChange> batch, CancellationToken cancellationToken)
        {
            try
            {
                // Coalesce per account, per field: apply in time order so the latest non-null value wins.
                var merged = new Dictionary<int, AccountStatusChange>();
                foreach (var change in batch.OrderBy(c => c.AtUtc))
                {
                    if (!merged.TryGetValue(change.AccountId, out var current))
                    {
                        merged[change.AccountId] = change;
                        continue;
                    }

                    merged[change.AccountId] = current with
                    {
                        UserId = change.UserId != 0 ? change.UserId : current.UserId,
                        ConnectionStatus = change.ConnectionStatus ?? current.ConnectionStatus,
                        IsExtensionConnected = change.IsExtensionConnected ?? current.IsExtensionConnected,
                        AuthStatus = change.AuthStatus ?? current.AuthStatus,
                        Reason = change.Reason ?? current.Reason,
                        AtUtc = change.AtUtc,
                    };
                }

                var ids = merged.Keys.ToList();

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var signalRService = scope.ServiceProvider.GetRequiredService<ISignalRService>();

                var accounts = await dbContext.Accounts
                                              .Where(a => ids.Contains(a.Id))
                                              .ToListAsync(cancellationToken);

                if (accounts.Count == 0)
                {
                    return;
                }

                var now = DateTime.UtcNow;
                var notificationsByUser = new Dictionary<int, UserAccountSignalRModel>();

                foreach (var account in accounts)
                {
                    var change = merged[account.Id];

                    // Ownership guard: only apply when the change came from the account's own user.
                    if (change.UserId != 0 && change.UserId != account.UserId)
                    {
                        // A dropped change here means a cross-user AccountId reference reached the queue
                        // (typically a stale cached AccountId after an API-key swap). The entry-point checks
                        // should reject it first; surface it to Sentry so a bypass is still visible.
                        SentrySdk.CaptureMessage(
                            $"Dropped status change: account {account.Id} owned by {account.UserId} but change stamped user {change.UserId}.",
                            SentryLevel.Warning);
                        continue;
                    }

                    if (change.ConnectionStatus.HasValue) account.ConnectionStatus = change.ConnectionStatus.Value;
                    if (change.IsExtensionConnected.HasValue) account.IsExtensionConnected = change.IsExtensionConnected.Value;
                    if (change.AuthStatus.HasValue) account.AuthStatus = change.AuthStatus.Value;
                    if (change.Reason.HasValue) account.Reason = change.Reason.Value;
                    account.UpdatedAt = now;

                    // Build the app notification from the account's resulting (full) state.
                    var statusModel = new AccountStatusSignalRModel
                    {
                        AccountId = account.Id,
                        AccountName = account.Name,
                        ConnectionStatus = account.ConnectionStatus,
                        ConnectionStatusText = account.ConnectionStatus.GetInfo().Name,
                        AuthStatus = account.AuthStatus,
                        AuthStatusText = account.AuthStatus.GetInfo().Name,
                        Reason = account.Reason,
                        ReasonText = account.Reason.GetInfo().Name,
                        IsConnected = account.ConnectionStatus == AccountConnectionStatus.Online,
                    };

                    if (!notificationsByUser.TryGetValue(account.UserId, out var userModel))
                    {
                        userModel = new UserAccountSignalRModel { AppId = account.UserId };
                        notificationsByUser[account.UserId] = userModel;
                    }

                    userModel.AccountsStatus.Add(statusModel);
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                if (notificationsByUser.Count > 0)
                {
                    await signalRService.NotifyAppAccountStatus(notificationsByUser.Values.ToList(), cancellationToken);
                }
            }
            catch (Exception)
            {
                // Propagate to ExecuteAsync → HandleFlushFailureAsync, which re-queues + backs off on a
                // transient DB failure (e.g. 57P01) and logs/drops a permanent one.
                throw;
            }
        }

        private static string WriteConflictDataToFile(List<AccountStatusChange> statuses, Exception ex)
        {
            try
            {
                const string dir = "Logs";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var fileName = $"account-status-flush-exception-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json";

                var payload = new
                {
                    TimestampUtc = DateTime.UtcNow,
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message,
                    Statuses = statuses,
                };

                File.WriteAllText(
                    Path.Combine(dir, fileName),
                    JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

                return fileName;
            }
            catch (Exception writeEx)
            {
                return $"(failed to write data file: {writeEx.Message})";
            }
        }
    }
}
