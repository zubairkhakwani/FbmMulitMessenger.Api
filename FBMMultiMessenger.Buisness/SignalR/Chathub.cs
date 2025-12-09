using AutoMapper;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Contracts.Contracts.LocalServer;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FBMMultiMessenger.Buisness.SignalR
{
    public class ChatHub : Hub
    {
        private static ConcurrentDictionary<string, ConnectionMetadata> _connections = new ConcurrentDictionary<string, ConnectionMetadata>();
        public static ConcurrentDictionary<string, string> _devices = new ConcurrentDictionary<string, string>();
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public ChatHub(IMediator mediator, IMapper mapper)
        {
            this._mediator=mediator;
            this._mapper=mapper;
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
                    _connections[Context.ConnectionId] = new ConnectionMetadata() { UserId = appId };

                    await Groups.AddToGroupAsync(Context.ConnectionId, appId);

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
                    var response = await _mediator.Send(new HandleLocalServerDisconnectionModelRequest() { LocalServerId = userId });
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
