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
        private readonly IMediator _mediator;

        public ChatHub(IMediator mediator)
        {
            this._mediator = mediator;
        }
        public async Task RegisterUser(string userId)
        {
            _connections[userId] = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        public async Task SendMessageToUser(string fromUserId, string toUserId, string message)
        {
            if (_connections.ContainsKey(toUserId))
            {
                await Clients.Client(_connections[toUserId]).SendAsync("ReceiveMessage", fromUserId, message);
            }

            if (_connections.ContainsKey(fromUserId))
            {
                await Clients.Client(_connections[fromUserId]).SendAsync("SendMessage", toUserId, message);
            }
        }

        // Notify extension about the message
        public async Task SendMessageToExtension(NotifyExtensionHttpRequest messageRequest)
        {
            // Send to the registered extension
            if (_connections.ContainsKey("Extension_User_123"))
            {
                await Clients.Client(_connections["Extension_User_123"])
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
