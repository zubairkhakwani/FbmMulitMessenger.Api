using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FBMMultiMessenger.Buisness.SignalR
{
    public class ChatHub : Hub
    {
        private static ConcurrentDictionary<string, string> _connections = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<string, string> _devices = new ConcurrentDictionary<string, string>();

        public async Task RegisterUser(string userId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    _connections[userId] = Context.ConnectionId;
                    await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                    await Groups.AddToGroupAsync(Context.ConnectionId, "AllServers");

                    Console.WriteLine($"User with id {userId} connected");
                }
            }
            catch (Exception ex)
            {

            }
        }

        public async Task MessageReceived(string toUserId, HandleChatHttpResponse message)
        {
            if (_connections.ContainsKey(toUserId))
            {
                await Clients.Client(_connections[toUserId]).SendAsync("ReceiveMessage", message);
            }
        }

        public void HandleNotification(string deviceId, string fbChatId)
        {
            if (!_devices.ContainsKey(deviceId))
            {
                _devices.TryAdd(deviceId, fbChatId);
            }
        }

        // Notify extension about the message
        public async Task NotifyExtension(string toExptensionId, NotifyLocalServerDTO messageRequest)
        {
            // Send to the registered extension
            if (_connections.ContainsKey(toExptensionId))
            {
                await Clients.Client(_connections[toExptensionId])
                    .SendAsync("SendMessage", messageRequest);
            }
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = _connections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (userId != null)
            {
                _connections.TryRemove(userId, out _);
                Console.WriteLine($"User with id {userId} disconnected");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
