using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.SignalR
{
    public class ChatHub : Hub
    {
        private static Dictionary<string, string> _connections = new Dictionary<string, string>();
        public async Task RegisterUser(string userId)
        {
            _connections[userId] = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        public async Task MessageReceived(string toUserId, ReceiveChatHttpResponse message)
        {
            if (_connections.ContainsKey(toUserId))
            {
                await Clients.Client(_connections[toUserId]).SendAsync("ReceiveMessage", message);
            }
        }

        // Notify extension about the message
        public async Task NotifyExtension(string toExptensionId, NotifyExtensionDTO messageRequest)
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
                _connections.Remove(userId);
                Console.WriteLine($"User {userId} disconnected");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
