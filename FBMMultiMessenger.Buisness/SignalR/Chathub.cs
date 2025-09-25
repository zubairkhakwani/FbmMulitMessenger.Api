using Microsoft.AspNetCore.SignalR;

namespace FBMMultiMessenger.Buisness.SignalR
{
    public class ChatHub : Hub
    {
        private static Dictionary<string, string> _connections = new Dictionary<string, string>();

        public async Task RegisterUser(string userId)
        {
            _connections[userId] = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            Console.WriteLine($"User {userId} registered with connection {Context.ConnectionId}");
        }

        public async Task SendMessageToUser(string fromUserId, string toUserId, string message)
        {
            if (_connections.ContainsKey(toUserId))
            {
                await Clients.Client(_connections[toUserId]).SendAsync("ReceiveMessage", fromUserId, message);
            }

            if (_connections.ContainsKey(fromUserId))
            {
                await Clients.Client(_connections[fromUserId]).SendAsync("MessageSent", toUserId, message);
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
