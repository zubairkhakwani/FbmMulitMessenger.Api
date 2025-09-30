using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FBMMultiMessenger.SignalR
{
    public class SignalRChatService
    {
        private HubConnection _hubConnection;
        public event Func<ReceiveChatHttpResponse, Task> OnMessageReceived;
        public event Func<SendChatMessagesHttpResponse, Task> OnMessageSent;

        public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

        private readonly string _baseURL;
        private string _currentUserId;

        public SignalRChatService(IConfiguration configuration)
        {
            _baseURL = configuration.GetValue<string>("Urls:BaseUrl")!;
        }

        public async Task ConnectAsync(string userId)
        {
            _currentUserId = userId;
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_baseURL}chathub")
                .Build();

            _hubConnection.On<ReceiveChatHttpResponse>("ReceiveMessage", async (messageData) =>
            {
                if (OnMessageReceived != null)
                {
                    await OnMessageReceived.Invoke(messageData);
                }
            });

            //TODO: CHECK IF THIS GETS CALLED WHEN YOU SEND MESSAGE VIA APP. IF YES THEN & WHY REMOVE IT.
            _hubConnection.On<SendChatMessagesHttpResponse>("SendMessage", (messageData) =>
            {
                OnMessageSent?.Invoke(messageData);
            });

            await _hubConnection.StartAsync();
            await _hubConnection.SendAsync("RegisterUser", userId);
        }

        public async Task SendMessageToSpecificUser(string toUserId, string message)
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.SendAsync("SendMessageToUser", _currentUserId, toUserId, message);
            }
        }

        public async Task DisconnectAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }
    }
}
