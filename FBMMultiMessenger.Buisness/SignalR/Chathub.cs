using FBMMultiMessenger.Buisness.Models.SignalR.Extension;
using FBMMultiMessenger.Buisness.Service.IServices;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FBMMultiMessenger.Buisness.SignalR
{
    public class ChatHub : Hub
    {
        public static ConcurrentDictionary<string, string> _devices = new ConcurrentDictionary<string, string>();
        private readonly ILocalServerService _localServerService;

        public ChatHub(ILocalServerService localServerService)
        {
            this._localServerService = localServerService;
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

                await _localServerService.HandleServerOnlineAsync(accountId, apiUserId);

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
                if (connectionMetadata.AccountId != null)
                {
                    await _localServerService.HandleServerOfflineAsync(connectionMetadata.AccountId.Value, connectionMetadata.APIUserId.Value);
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
