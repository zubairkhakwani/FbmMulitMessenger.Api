using FBMMultiMessenger.Buisness.Models.SignalR.Extension;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.StatusBatching;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Data.DB;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace FBMMultiMessenger.Buisness.SignalR
{
    public class ChatHub : Hub
    {
        public static ConcurrentDictionary<string, string> _devices = new ConcurrentDictionary<string, string>();
        private readonly IAccountStatusQueue _accountStatusQueue;
        private readonly ApplicationDbContext _dbContext;
        private readonly AccountActiveStatusCache _accountActiveStatusCache;

        public ChatHub(IAccountStatusQueue accountStatusQueue, ApplicationDbContext dbContext, AccountActiveStatusCache accountActiveStatusCache)
        {
            this._accountStatusQueue = accountStatusQueue;
            this._dbContext = dbContext;
            this._accountActiveStatusCache = accountActiveStatusCache;
        }

        public async Task RegisterLocalServer(string localServerId)
        {
            try
            {
                var metadata = new ConnectionMetadata()
                {
                    UserId = localServerId,
                    IsLocalServer = true,
                    ConnectedAt = DateTime.UtcNow
                };

                SingnalRConnectionManager._connections[Context.ConnectionId] = metadata;

                await Groups.AddToGroupAsync(Context.ConnectionId, localServerId);

                await Groups.AddToGroupAsync(Context.ConnectionId, "AllServers");

                //await _localServerService.HandleServerOnlineAsync(localServerId);

                Console.WriteLine($"User with id {localServerId} connected");
            }

            catch (Exception ex)
            {

            }
        }

        public async Task RegisterApp(string appId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(appId))
                {
                    SingnalRConnectionManager._connections[Context.ConnectionId] = new ConnectionMetadata() { UserId = appId };

                    await Groups.AddToGroupAsync(Context.ConnectionId, appId);

                    Console.WriteLine($"User with id {appId} connected");
                }
            }
            catch (Exception ex)
            {

            }
        }

        public async Task RegisterExtension(ExtensionConnectionSignalRModel request)
        {
            var accountId = request.AccountId;
            var apiUserId = request.UserId;

            try
            {
                // Reject accounts that were removed (soft-deleted): don't register, and tell the caller
                // to stop so it stops reconnecting/syncing. Cached (5-min TTL) so connect bursts don't
                // hit the DB per connection; removal updates the cache immediately.
                var isActiveAccount = await _accountActiveStatusCache.IsActiveAsync(accountId,
                    () => _dbContext.Accounts.AnyAsync(a => a.Id == accountId && a.IsActive));

                if (!isActiveAccount)
                {
                    await Clients.Caller.SendAsync("HandleAccountDeactivated", accountId);
                    Console.WriteLine($"Extension register rejected — account {accountId} is not active.");
                    return;
                }

                var extensionId = $"extension_{accountId}";

                SingnalRConnectionManager._connections[Context.ConnectionId] = new ConnectionMetadata() { ExtensionId = extensionId, AccountId = accountId, APIUserId = apiUserId };

                await Groups.AddToGroupAsync(Context.ConnectionId, extensionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, "AllExtensinos");

                // Presence is owned by the hub: queue an "online" change (batched write).
                _accountStatusQueue.Enqueue(new AccountStatusChange
                {
                    AccountId = accountId,
                    UserId = apiUserId,
                    ConnectionStatus = AccountConnectionStatus.Online,
                    IsExtensionConnected = true,
                    AuthStatus = AccountAuthStatus.LoggedIn,
                    Reason = AccountReason.ConnectedWithExtension,
                    AtUtc = DateTime.UtcNow,
                });

                Console.WriteLine($"Extension with id {extensionId} connected");
            }
            catch (Exception ex)
            {

            }
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionMetadata = SingnalRConnectionManager._connections.FirstOrDefault(x => x.Key == Context.ConnectionId).Value;

            if (connectionMetadata != null)
            {
                SingnalRConnectionManager._connections.TryRemove(Context.ConnectionId, out var _);

                var userId = connectionMetadata.UserId;
                //means it is the extension that is disconnecting
                if (connectionMetadata.AccountId != null && connectionMetadata.APIUserId != null)
                {
                    // A gone extension has no login state, so a disconnect owns both dimensions.
                    _accountStatusQueue.Enqueue(new AccountStatusChange
                    {
                        AccountId = connectionMetadata.AccountId.Value,
                        UserId = connectionMetadata.APIUserId.Value,
                        ConnectionStatus = AccountConnectionStatus.Offline,
                        IsExtensionConnected = false,
                        AuthStatus = AccountAuthStatus.NotConnected,
                        Reason = AccountReason.NotConnected,
                        AtUtc = DateTime.UtcNow,
                    });
                }

                Console.WriteLine($"User with id {userId} disconnected");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public class ConnectionMetadata
        {
            public string UserId { get; set; } = string.Empty;
            public int? APIUserId { get; set; }
            public string ExtensionId { get; set; } = string.Empty;
            public int? AccountId { get; set; }
            public bool IsLocalServer { get; set; }
            public DateTime ConnectedAt { get; set; }
        }
    }
}
