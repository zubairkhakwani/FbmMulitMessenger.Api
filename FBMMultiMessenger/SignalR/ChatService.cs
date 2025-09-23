using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.SignalR
{
    public class ChatService
    {
        private HubConnection _hubConnection;
        public event Action<string, string> OnMessageReceived;
        private readonly string _baseURL;
        public ChatService(IConfiguration configuration)
        {
            _baseURL = configuration.GetValue<string>("Urls:BaseUrl")!;
        }

        public async Task ConnectAsync()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_baseURL}chathub") 
                .Build();

            _hubConnection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                OnMessageReceived?.Invoke(user, message);
            });

            await _hubConnection.StartAsync();
        }

        public async Task SendMessage(string user, string message)
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.SendAsync("SendMessage", user, message);
            }
        }
    }
}
