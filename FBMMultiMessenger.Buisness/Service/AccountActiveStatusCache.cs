using System.Collections.Concurrent;

namespace FBMMultiMessenger.Buisness.Service
{
    /// <summary>
    /// In-memory, per-account cache so hot paths (extension registration and message sync) don't hit the
    /// database on every request. Besides active status it also caches the account's owner (UserId) and
    /// FB id (FbAccountId) so the sync path can verify, in-memory, that a request's accountId actually
    /// belongs to the current user and FB account (catching stale/cross-user accountIds). Short TTL; writes
    /// (removal / reactivation) update the entry immediately.
    /// </summary>
    public class AccountActiveStatusCache
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

        private readonly ConcurrentDictionary<int, Entry> _cache = new();

        private readonly record struct Entry(bool IsActive, int? UserId, string? FbAccountId, DateTime CheckedAtUtc);

        /// <summary>Active status + identity of an account, as cached/loaded by <see cref="GetAsync"/>.</summary>
        public readonly record struct CachedAccount(bool IsActive, int UserId, string? FbAccountId);

        /// <summary>
        /// Returns the cached active status if fresh; otherwise runs <paramref name="dbLookup"/>, caches the
        /// result (preserving any cached identity), and returns it.
        /// </summary>
        public async Task<bool> IsActiveAsync(int accountId, Func<Task<bool>> dbLookup)
        {
            if (_cache.TryGetValue(accountId, out var entry) &&
                DateTime.UtcNow - entry.CheckedAtUtc < Ttl)
            {
                return entry.IsActive;
            }

            var isActive = await dbLookup();
            _cache.TryGetValue(accountId, out var prev);
            _cache[accountId] = new Entry(isActive, prev.UserId, prev.FbAccountId, DateTime.UtcNow);
            return isActive;
        }

        /// <summary>
        /// Returns the account's active status + identity (UserId, FbAccountId), cached. <paramref name="dbLookup"/>
        /// should return null when the account does not exist. A cold/identity-less entry triggers a lookup.
        /// </summary>
        public async Task<CachedAccount?> GetAsync(int accountId, Func<Task<CachedAccount?>> dbLookup)
        {
            if (_cache.TryGetValue(accountId, out var entry) &&
                entry.UserId.HasValue &&
                DateTime.UtcNow - entry.CheckedAtUtc < Ttl)
            {
                return new CachedAccount(entry.IsActive, entry.UserId.Value, entry.FbAccountId);
            }

            var loaded = await dbLookup();
            if (loaded is null)
            {
                // Unknown account id — cache a short "inactive, no identity" so repeated bad ids don't hammer the DB.
                _cache[accountId] = new Entry(false, null, null, DateTime.UtcNow);
                return null;
            }

            _cache[accountId] = new Entry(loaded.Value.IsActive, loaded.Value.UserId, loaded.Value.FbAccountId, DateTime.UtcNow);
            return loaded;
        }

        /// <summary>Immediately sets the cached active status (used on account removal / reactivation).</summary>
        public void Set(int accountId, bool isActive)
        {
            _cache.TryGetValue(accountId, out var prev);
            _cache[accountId] = new Entry(isActive, prev.UserId, prev.FbAccountId, DateTime.UtcNow);
        }
    }
}
