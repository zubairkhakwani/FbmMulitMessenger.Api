using Microsoft.AspNetCore.SignalR;

namespace FBMMultiMessenger.Server.SignalR
{
    public class Chathub : Hub
    {
        public async Task SendMessage(string user , string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
