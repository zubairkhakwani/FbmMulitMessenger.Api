using System.Collections.Concurrent;
using static FBMMultiMessenger.Buisness.SignalR.ChatHub;

namespace FBMMultiMessenger.Buisness.SignalR
{
    public static class SingnalRConnectionManager
    {
        public static readonly ConcurrentDictionary<string, ConnectionMetadata> _connections = new ConcurrentDictionary<string, ConnectionMetadata>();


        // Whether any live extension socket is currently registered for this account. False means the DB's
        // IsExtensionConnected flag is stale (no real socket) — e.g. an ungraceful shutdown left it set.
        // Note: this reflects only THIS server instance's in-memory state.
        public static bool HasLiveExtensionConnection(int accountId)
        {
            return _connections.Values.Any(m => m.AccountId == accountId && !string.IsNullOrEmpty(m.ExtensionId));
        }

        public static List<int> GetDisconnectedAccountsIds(Dictionary<int, int> accountsIdsToCheck)
        {
            var connected = _connections.Values
                .Where(x => x.AccountId.HasValue && x.APIUserId.HasValue)
                .Select(x => (AccountId: x.AccountId.Value, UserId: x.APIUserId.Value))
                .ToHashSet();

            return accountsIdsToCheck
                .Where(x => !connected.Contains((x.Key, x.Value)))
                .Select(x => x.Key)
                .ToList();
        }
    }
}
