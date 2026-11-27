using System.Collections.Concurrent;
using FBMMultiMessenger.Buisness.SignalR;
using Sentry;

namespace FBMMultiMessenger.Buisness.Service
{
    /// <summary>
    /// Resolves an account's extension-connected status from the live SignalR connection rather than the
    /// DB's IsExtensionConnected bit. The batched status-flush service has been observed to leave that bit
    /// stale (extension actually connected, but the DB shows offline). Until that's fixed, the live socket is
    /// treated as the source of truth, and any disagreement between the DB bit and the live connection is
    /// reported to Sentry so the underlying drift stays visible.
    ///
    /// NOTE: presence is read from the in-memory SignalR connection map, which is per server instance. This
    /// assumes a single instance — no web garden / scale-out without a SignalR backplane. On multiple
    /// instances an account connected to another instance would resolve as disconnected here.
    /// </summary>
    public static class ExtensionPresenceResolver
    {
        // Status is polled frequently; a persistent drift would otherwise raise a Sentry event on every poll.
        // Report each account's drift at most once per window.
        private static readonly ConcurrentDictionary<int, DateTime> _lastDriftReportUtc = new();
        private static readonly TimeSpan DriftReportInterval = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Returns the live extension-connected value for <paramref name="accountId"/>, and reports to Sentry
        /// (throttled) when it disagrees with the DB's <paramref name="dbConnected"/> bit.
        /// </summary>
        public static bool ResolveIsConnected(int accountId, int userId, bool dbConnected)
        {
            var liveConnected = SingnalRConnectionManager.HasLiveExtensionConnection(accountId);

            if (liveConnected != dbConnected)
            {
                ReportDrift(accountId, userId, dbConnected, liveConnected);
            }

            return liveConnected;
        }

        private static void ReportDrift(int accountId, int userId, bool dbConnected, bool liveConnected)
        {
            var now = DateTime.UtcNow;
            if (_lastDriftReportUtc.TryGetValue(accountId, out var last) && now - last < DriftReportInterval)
            {
                return;
            }
            _lastDriftReportUtc[accountId] = now;

            SentrySdk.CaptureMessage(
                $"Extension-connected drift: account {accountId} (user {userId}) — DB IsExtensionConnected={dbConnected} " +
                $"but live SignalR={liveConnected}. Returning the live value.",
                SentryLevel.Warning);
        }
    }
}
