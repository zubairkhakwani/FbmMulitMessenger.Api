using FBMMultiMessenger.Buisness.Service.IServices;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FBMMultiMessenger.Buisness.SignalR
{
    public class ChatHub : Hub
    {
        private static ConcurrentDictionary<string, ConnectionMetadata> _connections = new ConcurrentDictionary<string, ConnectionMetadata>();
        public static ConcurrentDictionary<string, string> _devices = new ConcurrentDictionary<string, string>();
        private readonly ILocalServerService _localServerService;

        public ChatHub(ILocalServerService localServerService)
        {
            this._localServerService=localServerService;
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

                _connections[Context.ConnectionId] = metadata;

                await Groups.AddToGroupAsync(Context.ConnectionId, localServerId);

                await Groups.AddToGroupAsync(Context.ConnectionId, "AllServers");

                await _localServerService.HandleServerOnlineAsync(localServerId);

                Console.WriteLine($"User with id {localServerId} connected");
            }

            catch (Exception ex)
            {

            }
        }

        public async Task RegisterApp(string appId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(appId))
                {
                    _connections[Context.ConnectionId] = new ConnectionMetadata() { UserId = appId };

                    await Groups.AddToGroupAsync(Context.ConnectionId, appId, cancellationToken);

                    Console.WriteLine($"User with id {appId} connected");
                }
            }
            catch (Exception ex)
            {

            }
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionMetadata = _connections.FirstOrDefault(x => x.Key == Context.ConnectionId).Value;

            if (connectionMetadata != null)
            {
                _connections.TryRemove(Context.ConnectionId, out var _);

                var userId = connectionMetadata.UserId;
                if (connectionMetadata.IsLocalServer)
                {
                    await _localServerService.HandleServerOfflineAsync(userId);
                }

                Console.WriteLine($"User with id {userId} disconnected");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public class ConnectionMetadata
        {
            public string UserId { get; set; } = string.Empty;
            public bool IsLocalServer { get; set; }
            public DateTime ConnectedAt { get; set; }
        }
    }
}
