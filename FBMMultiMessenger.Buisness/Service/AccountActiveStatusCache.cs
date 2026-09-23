using System.Collections.Concurrent;

namespace FBMMultiMessenger.Buisness.Service
{
    /// <summary>
    /// In-memory, per-account cache of whether an account is active, so hot paths (extension registration
    /// and message sync) don't hit the database on every request. Reads use a short TTL; writes
    /// (account removed / reactivated) update the entry immediately so removal is enforced without lag.
    /// </summary>
    public class AccountActiveStatusCache
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

        private readonly ConcurrentDictionary<int, Entry> _cache = new();

        private readonly record struct Entry(bool IsActive, DateTime CheckedAtUtc);

        /// <summary>
        /// Returns the cached active status if it is still fresh; otherwise runs <paramref name="dbLookup"/>,
        /// caches the result, and returns it. A rare duplicate lookup on a cold entry is harmless.
        /// </summary>
        public async Task<bool> IsActiveAsync(int accountId, Func<Task<bool>> dbLookup)
        {
            if (_cache.TryGetValue(accountId, out var entry) &&
                DateTime.UtcNow - entry.CheckedAtUtc < Ttl)
            {
                return entry.IsActive;
            }

            var isActive = await dbLookup();
            _cache[accountId] = new Entry(isActive, DateTime.UtcNow);
            return isActive;
        }

        /// <summary>Immediately sets the cached status (used on account removal / reactivation).</summary>
        public void Set(int accountId, bool isActive)
        {
            _cache[accountId] = new Entry(isActive, DateTime.UtcNow);
        }
    }
}
