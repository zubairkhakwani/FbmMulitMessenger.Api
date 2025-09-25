using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace FBMMultiMessenger.Components.Pages.Chat
{
    public partial class Chat : IDisposable
    {

        [Inject]
        public IAccountService AccountService { get; set; }

        [Inject]
        public IChatMessagesService ChatMessagesService { get; set; }

        [Inject]
        public SignalRChatService SignalRChatService { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public IJSRuntime JS { get; set; }

        private string Message = string.Empty;
        private int? ChatId = null;
        private string? selectedFbChatId = null;
        private string currentUserId = "User123"; //TODO - Authentication

        public List<GetMyChatsHttpResponse> MyAccountChats = new List<GetMyChatsHttpResponse>();
        public List<GeChatMessagesHttpResponse> MyChatMessages = new List<GeChatMessagesHttpResponse>();

        protected override async Task OnInitializedAsync()
        {
            await ConnectToSignalR();

            await GetAccountChats();

        }

        public async Task GetAccountChats()
        {
            var response = await AccountService.GetMyChats<BaseResponse<GetAllMyAccountsChatsHttpResponse>>();
            if (response is null ||  !response.IsSuccess)
            {
                Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Error);

                return;
            }

            MyAccountChats = response?.Data?.Chats ?? new List<GetMyChatsHttpResponse>();
        }

        public async Task ConnectToSignalR()
        {
            if (!SignalRChatService.IsConnected)
            {
                await SignalRChatService.ConnectAsync(currentUserId);

                SignalRChatService.OnMessageReceived += async (msg) => await HandleMessageReceived(msg);
            }
        }

        private async Task HandleMessageReceived(ReceiveChatHttpResponse receivedChat)
        {
            var isChatPresent = MyAccountChats.FirstOrDefault(x => x.FbChatId == receivedChat.FbChatId);
            if (isChatPresent is null)
            {
                //sidebar account chat
                var newChat = new GetMyChatsHttpResponse()
                {
                    Id = receivedChat.ChatId,
                    FbLisFbListingTitle = receivedChat.FbListingTitle,
                    FbListingLocation = receivedChat.FbListingLocation,
                    FbListingPrice = receivedChat.FbListingPrice,
                    FbChatId = receivedChat.FbChatId,
                    StartedAt = receivedChat.StartedAt,
                    IsRead = receivedChat.IsRead
                };

                MyAccountChats.Add(newChat);

            }

            //If the message that we received fbChatId is not opened we add a unread badge otherwise we add new chat so it can be displayed in the messages.
            if (receivedChat.FbChatId != selectedFbChatId)
            {
                var myAccountChat = MyAccountChats.FirstOrDefault(x => x.FbChatId == receivedChat.FbChatId);
                if (myAccountChat is not null)
                {
                    myAccountChat.UnReadCount += 1;
                }
            }
            else
            {
                //actual chat message
                var receivedMessage = new GeChatMessagesHttpResponse()
                {
                    Message = receivedChat.Message,
                    IsReceived = true,
                    CreatedAt = DateTime.UtcNow
                };

                MyChatMessages.Add(receivedMessage);
            }


            //Force UI to reload, so our changes reflect.
            await InvokeAsync(StateHasChanged);

            //Display newest message on top.
            MyAccountChats =  MyAccountChats.OrderByDescending(x => x.StartedAt).ToList();

            await JS.InvokeVoidAsync("myInterop.playNotificationSound", 1);
        }


        public async Task LoadChatMessage(int chatId, string fbChatId)
        {
            var myAccountChats = MyAccountChats.FirstOrDefault(x => x.FbChatId == fbChatId);
            if (myAccountChats is not null)
            {
                //Making the unread messages to read
                myAccountChats.UnReadCount = 0;

                //Force UI to reload, so our changes reflect.
                await InvokeAsync(StateHasChanged);
            }

            var response = await ChatMessagesService.GetChatMessages<BaseResponse<List<GeChatMessagesHttpResponse>>>(chatId);

            if (response is null || !response.IsSuccess)
            {
                Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Error);

                return;
            }

            ChatId = chatId;
            selectedFbChatId = fbChatId;

            MyChatMessages  = response?.Data ?? new List<GeChatMessagesHttpResponse>();
        }

        public async Task SendMessage()
        {
            Message = Message.Trim();

            if (!string.IsNullOrWhiteSpace(Message) && ChatId != 0)
            {
                //This is for UI.
                var newChat = new GeChatMessagesHttpResponse()
                {

                    IsReceived = false,
                    Message = Message,
                    CreatedAt = DateTime.UtcNow
                };

                MyChatMessages.Add(newChat);


                //This is to call API 
                var request = new SendChatMessageHttpRequest()
                {
                    ChatId = ChatId,
                    Message = Message
                };

                var response = await ChatMessagesService.SendChatMessage<BaseResponse<SendChatMessageHttpResponse>>(request);

                if (response is not null && response.IsSuccess)
                {
                    Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Success);
                    Message = string.Empty;
                    return;
                }

                Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Error);
            }
        }

        public async Task HandleKeyDownPress(KeyboardEventArgs e)
        {
            //if (e.Key == "Enter" && e.ShiftKey)
            //{
            //    Message+="\n";
            //}
            //if (e.Key == "Enter")
            //{
            //    await SendMessage();
            //}

        }

        public void Dispose()
        {
            SignalRChatService?.DisconnectAsync();
        }
    }
}
