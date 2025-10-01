using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Extensions;
using System.Text.Json;

namespace FBMMultiMessenger.Components.Pages.Chat
{
    public partial class Chat : IDisposable
    {

        [Inject]
        public IAccountService AccountService { get; set; }

        [Inject]
        public IChatMessagesService ChatMessagesService { get; set; }

        [Inject]
        public IExtensionService ExtensionService { get; set; }

        [Inject]
        public SignalRChatService SignalRChatService { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        public IJSRuntime JS { get; set; }

        private const int MaxMediaCount = 100;
        private const int MaxMediaSize = 25 * 1024 * 1024; // 1024 * 1024 == 1mb hence total 25mb.

        private string Message = string.Empty;
        private List<FileData> PreviewMediaFiles { get; set; } = new List<FileData>();
        private List<FileData> PreviewMediaInMessagesContainer = new List<FileData>();
        private bool IsNotified = false;

        private string? SelectedFbChatId = null;
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

                SignalRChatService.OnMessageReceived += async (msg) => await HandleMessageReceivedAsync(msg);
                SignalRChatService.OnMessageSent += async (msg) => await HandleMessageSentAsync(msg);
            }
        }

        private List<FileData> GetImages(string message)
        {
            var imagesList = JsonSerializer.Deserialize<List<string>>(message);

            var fileModel = imagesList.Select(i => new FileData()
            {
                FileUrl = i

            }).ToList();

            return fileModel;
        }



        private async Task HandleMessageReceivedAsync(ReceiveChatHttpResponse receivedChat)
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
                    IsRead = receivedChat.IsRead,
                };

                MyAccountChats.Add(newChat);

            }

            //If the message that we received fbChatId is not opened so we add a unread badge otherwise we add new chat so it can be displayed in the messages.
            if (receivedChat.FbChatId != SelectedFbChatId)
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
                    IsReceived = true,
                    IsTextMessage = receivedChat.IsTextMessage,
                    IsImageMessage = receivedChat.IsImageMessage,
                    IsVideoMessage = receivedChat.IsVideoMessage,
                    CreatedAt = DateTime.UtcNow
                };

                if (receivedChat.IsTextMessage)
                {
                    receivedMessage.Message = receivedChat.Message;
                }
                else if (receivedChat.IsImageMessage)
                {
                    receivedMessage.FileData = GetImages(receivedChat.Message);
                }

                MyChatMessages.Add(receivedMessage);
            }


            //Force UI to reload, so our changes reflect.
            await InvokeAsync(StateHasChanged);

            //Display newest message on top.
            MyAccountChats =  MyAccountChats.OrderByDescending(x => x.StartedAt).ToList();

            await JS.InvokeVoidAsync("myInterop.playNotificationSound", 1);
        }


        //TODO
        private async Task HandleMessageSentAsync(SendChatMessagesHttpResponse messageSent)
        {
            var message = MyChatMessages.FirstOrDefault(x => x.FBChatId == messageSent.FbChatId
                                                            && (messageSent.IsTextMessage  && x.Message == messageSent.Message));
            if (message is not null)
            {
                message.Sending = false;
            }



            //TODO: When message is send from facebook not from our app so we have to inject that message.
            //actual chat message
            //var receivedMessage = new GeChatMessagesHttpResponse()
            //{
            //    Message = messageSent.Message,
            //    IsReceived = false,
            //    IsSent = true,
            //    IsTextMessage = messageSent.IsTextMessage,
            //    IsImageMessage = messageSent.IsImageMessage,
            //    IsVideoMessage = messageSent.IsVideoMessage,
            //    IsAudioMessage = messageSent.IsAudioMessage,
            //    CreatedAt = DateTime.UtcNow
            //};

            //MyChatMessages.Add(receivedMessage);



            //Force UI to reload, so our changes reflect.
            await InvokeAsync(StateHasChanged);
        }


        public async Task LoadChatMessage(string fbChatId)
        {
            var myAccountChats = MyAccountChats.FirstOrDefault(x => x.FbChatId == fbChatId);
            if (myAccountChats is not null)
            {
                //Making the unread messages to read
                myAccountChats.UnReadCount = 0;

                //Force UI to reload, so our changes reflect.
                await InvokeAsync(StateHasChanged);
            }

            var response = await ChatMessagesService.GetChatMessages<BaseResponse<List<GeChatMessagesHttpResponse>>>(fbChatId);

            if (response is null || !response.IsSuccess)
            {
                Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Error);

                return;
            }

            SelectedFbChatId = fbChatId;

            MyChatMessages  = response?.Data ?? new List<GeChatMessagesHttpResponse>();
        }

        public async Task NotifyExtension()
        {
            var isValidRequest = IsValidRequest();
            if (!isValidRequest)
            {
                return;
            }

            Message = Message.Trim();

            //This is for UI.
            var newChat = new GeChatMessagesHttpResponse()
            {
                FBChatId = SelectedFbChatId!,
                Message = Message,
                IsReceived = false,
                IsSent = true,
                IsTextMessage = !string.IsNullOrWhiteSpace(Message),
                IsImageMessage = PreviewMediaFiles.Count > 0,
                IsVideoMessage = false,
                IsAudioMessage = false,
                CreatedAt = DateTime.UtcNow,
                Sending = true,
                UniqueId = Guid.NewGuid().ToString()
            };

            if (newChat.IsImageMessage)
            {
                newChat.FileData = PreviewMediaFiles;
                PreviewMediaFiles = new();
            }

            MyChatMessages.Add(newChat);


            //This is to call API 
            var request = new NotifyExtensionRequest()
            {
                FbChatId = SelectedFbChatId!,
                Message = Message,
                Files = newChat.FileData.Select(x => x.File).ToList()
            };

            var response = await ExtensionService.Notify<BaseResponse<NotifyExtensionHttpResponse>>(request);

            if (response is not null && response.IsSuccess)
            {
                Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Success);
                Message = string.Empty;
                PreviewMediaFiles = new List<FileData>();
                return;
            }

            Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Error);

        }

        public bool IsValidRequest()
        {
            if (PreviewMediaFiles.Count == 0 && string.IsNullOrWhiteSpace(Message))
            {
                return false;
            }
            return true;
        }

        public async Task HandleFileUpload(InputFileChangeEventArgs e)
        {
            var files = e.GetMultipleFiles();

            if (files.Count > MaxMediaCount)
            {
                await JS.InvokeVoidAsync("myInterop.handleMediaFailed", "Unable to attach media", "You can only attach a maximum of 100 media to a single message.");

                return;
            }

            var totalSize = files.Sum(f => f.Size);

            if (totalSize > MaxMediaSize)
            {
                await JS.InvokeVoidAsync("myInterop.handleMediaFailed",
                    "Upload Failed",
                    $"The total size of selected files ({totalSize / (1024 * 1024)} MB) exceeds the {MaxMediaSize / (1024 * 1024)} MB limit.");
                return;
            }


            foreach (var file in files)
            {
                using var ms = new MemoryStream();
                await file.OpenReadStream(MaxMediaSize).CopyToAsync(ms);
                var buffer = ms.ToArray();

                var newFile = new FileData()
                {
                    Id = $"File-{Guid.NewGuid()}",
                    File = file,
                    FileName = file.Name,
                    FileUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer)}",
                    IsVideo = file.ContentType.StartsWith("video/")
                };

                PreviewMediaFiles.Add(newFile);
            }
        }

        public void HandleFileRemoval(string id)
        {
            var file = PreviewMediaFiles.FirstOrDefault(x => x.Id == id);
            if (file is not null)
            {
                PreviewMediaFiles.Remove(file);
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
