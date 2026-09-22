using FBMMultiMessenger.Buisness.Models.SignalR.Extension;
using FBMMultiMessenger.Buisness.Service.StatusBatching;
using FBMMultiMessenger.Contracts.Enums;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FBMMultiMessenger.Buisness.SignalR
{
    public class ChatHub : Hub
    {
        public static ConcurrentDictionary<string, string> _devices = new ConcurrentDictionary<string, string>();
        private readonly IAccountStatusQueue _accountStatusQueue;

        public ChatHub(IAccountStatusQueue accountStatusQueue)
        {
            this._accountStatusQueue = accountStatusQueue;
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
