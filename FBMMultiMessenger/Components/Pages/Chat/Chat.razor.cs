using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Notification;
using FBMMultiMessenger.Services;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using OneSignalSDK.DotNet;
using System.Text.Json;
using System.Threading.Tasks;


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

        [Inject]
        private NavigationManager Navigation { get; set; }

        [Inject]
        private BackButtonService BackButtonService { get; set; }

        [Inject]
        private OneSignalService OneSignalService { get; set; }

        [SupplyParameterFromQuery]
        public string IsNotification { get; set; } //this bit tells if the user opens the notification from his app and we have to show him the right chat.

        [SupplyParameterFromQuery]
        public string FbChatId { get; set; }


        //For Media files
        private const int MaxMediaCount = 100;
        private const int MaxMediaSize = 25 * 1024 * 1024; // 1024 * 1024 == 1mb hence total 25mb.

        //The actual message 
        private string Message = string.Empty;

        private List<FileData> PreviewMediaFiles { get; set; } = new List<FileData>();
        private List<FileData> PreviewMediaInMessagesContainer = new List<FileData>();
        private bool IsNotified = false;
        private bool IsLoading = true;

        private string? SelectedFbChatId = null;
        private string currentUserId = "User123"; //TODO - Authentication
        private bool isAndriodPlatform;

        private string _filterKeyword = string.Empty;
        private string FilterKeyword
        {
            get => _filterKeyword;
            set
            {
                _filterKeyword = value;
                FilterChat();
            }
        }

        //For Mobile layout we overlap the side bar and messages
        private int SidebarZIndex = 100;
        private int MainChatZIndex = 0;

        //Selected Message Header
        private string selectedListingTitle = "John Doe";
        private string selectedListingLocation = "London";
        private string selectedListingPrice = "100";


        public List<GetMyChatsHttpResponse> FilteredAccountChats = new List<GetMyChatsHttpResponse>();
        public List<GetMyChatsHttpResponse> AccountChats = new List<GetMyChatsHttpResponse>();

        public List<GeChatMessagesHttpResponse> ChatMessages = new List<GeChatMessagesHttpResponse>();
        protected override async Task OnInitializedAsync()
        {
            isAndriodPlatform =  DeviceInfo.Platform != DevicePlatform.WinUI;

            BackButtonService.BackButtonPressed+= OnBackButtonPressed;

            if (!string.IsNullOrWhiteSpace(IsNotification) && !string.IsNullOrWhiteSpace(FbChatId))
            {
                await LoadChatMessage(FbChatId);
            }

            var taskSignalR = ConnectToSignalR();

            var taskAccountsQuery = GetAccountChats();

            await Task.WhenAll(taskSignalR, taskAccountsQuery);

            await ConfigurePushNotificationsAsync();

            await JS.InvokeVoidAsync("registerEnterHandler", DotNetObjectReference.Create(this));
        }

        private void OnBackButtonPressed()
        {
            // Handles the Android back button press behavior.
            // If the sidebar is currently active (visible above the main chat), navigate back to the Account page.
            // Otherwise, if the user is viewing chat messages, toggle back to the sidebar view instead.

            if (SidebarZIndex > MainChatZIndex)
            {
                Navigation.NavigateTo("/Account");
                return;
            }

            HandleMobileMainChat();
            StateHasChanged();
        }

        public async Task GetAccountChats()
        {
            var response = await AccountService.GetMyChats<BaseResponse<GetAllMyAccountsChatsHttpResponse>>();
            IsLoading = false;
            if (response is null ||  !response.IsSuccess)
            {
                Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Error);

                return;
            }

            FilteredAccountChats = AccountChats = response?.Data?.Chats ?? new List<GetMyChatsHttpResponse>();
        }



        private List<FileData> GetFileData(string message)
        {
            var mediaUrls = JsonSerializer.Deserialize<List<string>>(message);

            var fileModel = mediaUrls?.Select(url => new FileData()
            {
                FileUrl = url,
                IsVideo = IsVideo(url)

            }).ToList();

            return fileModel ?? new List<FileData>();
        }

        private bool IsVideo(string url)
        {
            var videoExtensions = new[] { ".mp4", ".webm", ".ogg", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".m4v" };

            try
            {
                var uri = new Uri(url);
                var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
                return videoExtensions.Contains(extension);
            }
            catch
            {
                return false;
            }
        }

        private async Task HandleMessageReceivedAsync(HandleChatHttpResponse receivedChat)
        {
            var isChatPresent = FilteredAccountChats.FirstOrDefault(x => x.FbChatId == receivedChat.FbChatId);
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

                FilteredAccountChats.Insert(0, newChat);

            }

            //If the message that we received fbChatId is not opened so we add an unread badge and show it on top of the chat.
            if (receivedChat.FbChatId != SelectedFbChatId)
            {
                var myAccountChat = FilteredAccountChats.FirstOrDefault(x => x.FbChatId == receivedChat.FbChatId);
                if (myAccountChat is not null)
                {
                    FilteredAccountChats.Remove(myAccountChat);
                    myAccountChat.UnReadCount += 1;
                    FilteredAccountChats.Insert(0, myAccountChat); //new message should display on top.
                    await JS.InvokeVoidAsync("myInterop.playNotificationSound", 1);
                }
            }
            else
            {
                //actual chat message
                var receivedMessage = new GeChatMessagesHttpResponse()
                {
                    IsReceived = !receivedChat.IsSent,
                    IsTextMessage = receivedChat.IsTextMessage,
                    IsImageMessage = receivedChat.IsImageMessage,
                    IsVideoMessage = receivedChat.IsVideoMessage,
                    IsAudioMessage = receivedChat.IsAudioMessage,
                    CreatedAt = receivedChat.StartedAt
                };

                if (receivedChat.IsImageMessage || receivedChat.IsVideoMessage)
                {
                    receivedMessage.FileData = GetFileData(receivedChat.Message);
                }
                else
                {
                    receivedMessage.Message = receivedChat.Message;
                }

                ChatMessages.Add(receivedMessage);
            }

            await InvokeAsync(StateHasChanged);
        }

        public async Task LoadChatMessage(string fbChatId)
        {
            HandleSelectedChat(fbChatId);

            if (isAndriodPlatform)
            {
                HandleMobileSideBar();
                await HandlePushNotification(fbChatId);
            }


            var myAccountChats = FilteredAccountChats.FirstOrDefault(x => x.FbChatId == fbChatId);
            if (myAccountChats is not null)
            {
                //Making the unread messages to read
                myAccountChats.UnReadCount = 0;
                await InvokeAsync(StateHasChanged);
            }

            var response = await ChatMessagesService.GetChatMessages<BaseResponse<List<GeChatMessagesHttpResponse>>>(fbChatId);

            if (response is null || !response.IsSuccess)
            {
                Snackbar.Add(response?.Message ?? "Hmm, looks like something went wrong please contact administrator.", Severity.Error);

                return;
            }

            SelectedFbChatId = fbChatId;

            var responseChatMesasge = response?.Data ?? new List<GeChatMessagesHttpResponse>();

            foreach (var chatMessge in responseChatMesasge)
            {
                if (chatMessge.IsImageMessage || chatMessge.IsVideoMessage)
                {
                    chatMessge.FileData = GetFileData(chatMessge.Message);
                }
            }

            ChatMessages  = response?.Data ?? new List<GeChatMessagesHttpResponse>();
        }

        public async Task HandlePushNotification(string fbChatId)
        {
            if (isAndriodPlatform)
            {
                var deviceId = OneSignal.User.PushSubscription.Id;
                await SignalRChatService.HandleNotification(deviceId, fbChatId);
            }
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

            ChatMessages.Add(newChat);
            Message = string.Empty;

            //This is to call API 
            var request = new NotifyExtensionRequest()
            {
                FbChatId = SelectedFbChatId!,
                Message = newChat.Message,
                Files = newChat.FileData.Select(x => x.File).ToList()
            };

            var response = await ExtensionService.Notify<BaseResponse<NotifyExtensionHttpResponse>>(request);

            if (!response.IsSuccess && response.RedirectToPackages)
            {
                var isSubscriptionExpired = response.Data?.IsSubscriptionExpired ?? false;
                Navigation.NavigateTo($"/packages?isExpired={isSubscriptionExpired}&message={response.Message}");
                return;
            }

            else if (response.IsSuccess)
            {
                //Snackbar.Add(response.Message, Severity.Success);
                //Message = string.Empty;
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
            try
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
                    //if base64 is needed you can uncomment.
                    using var ms = new MemoryStream();
                    await file.OpenReadStream(MaxMediaSize).CopyToAsync(ms);
                    var buffer = ms.ToArray();
                    var bas64 = $"data:{file.ContentType};base64,{Convert.ToBase64String(buffer)}";

                    // get a blob URL 
                    // var objectUrl = await JS.InvokeAsync<string>("myInterop.createObjectURL", file);
                    var newFile = new FileData()
                    {
                        Id = $"File-{Guid.NewGuid()}",
                        File = file,
                        FileName = file.Name,
                        FileUrl = bas64,
                        IsVideo = file.ContentType.StartsWith("video/")
                    };

                    PreviewMediaFiles.Add(newFile);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Failed to select your file", Severity.Error);
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


        [JSInvokable]
        public async Task HandleEnterKey(string message)
        {
            Message = message;
            await NotifyExtension();
            await InvokeAsync(StateHasChanged);
        }

        private void HandleSelectedChat(string fbChatId)
        {
            // Updates the main chat header with the listing details (title, location, and price)
            // of the chat selected by the user.

            var chat = FilteredAccountChats.FirstOrDefault(x => x.FbChatId == fbChatId);
            if (chat is not null)
            {
                selectedListingTitle = chat.FbLisFbListingTitle;
                selectedListingLocation = chat.FbListingLocation;
                selectedListingPrice  = chat.FbListingPrice.ToString();
            }
        }

        private void HandleMobileMainChat()
        {
            // Displays the sidebar view on mobile by resetting the selected chat
            // and bringing the sidebar to the front.
            SelectedFbChatId = null;
            SidebarZIndex = 100;
            MainChatZIndex = 0;
        }

        private void HandleMobileSideBar()
        {
            // Displays the main chat view on mobile by bringing the chat section
            // to the front and hiding the sidebar.
            SidebarZIndex = 0;
            MainChatZIndex = 110;

        }

        private async Task ConfigurePushNotificationsAsync()
        {
            if (isAndriodPlatform)
            {
                await OneSignalService.AskNotificationPermissionAsync();
                OneSignalService.OnNotificationClicked();

                // Optional
                var playerId = OneSignal.User.PushSubscription.Id;
                Console.WriteLine("Player Id :", playerId);
            }
        }

        public async Task ConnectToSignalR()
        {
            if (!SignalRChatService.IsConnected)
            {
                await SignalRChatService.ConnectAsync(currentUserId);

                SignalRChatService.OnMessageReceived += async (msg) => await HandleMessageReceivedAsync(msg);
            }
        }

        private void FilterChat()
        {
            var filteredAccountChats = AccountChats.Where(x => x.FbLisFbListingTitle.ToLower().Contains(FilterKeyword)
                                        ||
                                        x.FbListingPrice.ToString().Contains(FilterKeyword)
                                        ||
                                        x.FbListingLocation.ToLower().Contains(FilterKeyword)).ToList();

            FilteredAccountChats = new List<GetMyChatsHttpResponse>(filteredAccountChats);

            StateHasChanged();

        }

        public void Dispose()
        {
            BackButtonService.BackButtonPressed -= OnBackButtonPressed;
            SignalRChatService.DisconnectAsync();
        }
    }
}
