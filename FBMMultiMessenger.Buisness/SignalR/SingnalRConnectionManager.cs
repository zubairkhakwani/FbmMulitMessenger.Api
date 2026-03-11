using System.Collections.Concurrent;
using static FBMMultiMessenger.Buisness.SignalR.ChatHub;

namespace FBMMultiMessenger.Buisness.SignalR
{
    public static class SingnalRConnectionManager
    {
        public static readonly ConcurrentDictionary<string, ConnectionMetadata> _connections = new ConcurrentDictionary<string, ConnectionMetadata>();


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
