using FBMMultiMessenger.Components.Shared.CustomPopupform;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OneSignalSDK.DotNet.Core.Notifications;
using Color = MudBlazor.Color;
using OneSignalSDK.DotNet;
using Microsoft.Extensions.Configuration;
using OneSignalSDK.DotNet.Core.Debug;


namespace FBMMultiMessenger.Components.Pages.Account
{
    public partial class Account
    {
        [Inject]
        private IAccountService AccountService { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }
        [Inject]
        private ITokenProvider TokenProvider { get; set; }

        [Inject]
        public IDialogService DialogService { get; set; }

        [Inject]
        private ISnackbar Snackbar { get; set; }

        [Inject]
        private IConfiguration Configuration { get; set; }

        [SupplyParameterFromQuery]
        public string? Message { get; set; }

        private MudTable<GetMyAccountsHttpResponse> table;

        protected override async Task OnInitializedAsync()
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                //Initialize one signal for push notifiactions
                InitializeOneSignal();
            }
            string? token = await TokenProvider.GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                Navigation.NavigateTo("/login");
            }

            if (!string.IsNullOrWhiteSpace(Message))
            {
                Snackbar.Add(Message, Severity.Success);
            }
        }
        private void InitializeOneSignal()
        {
            var appId = Configuration.GetValue<string>("OneSignal:AppId")!;
            OneSignal.Debug.LogLevel = LogLevel.VERBOSE;

            OneSignal.Initialize(appId);

            OneSignal.Notifications.RequestPermissionAsync(true);

            OneSignal.Notifications.Clicked += OnNotificationClicked;

            // Optional
            var playerId = OneSignal.User.PushSubscription.Id;
            System.Diagnostics.Debug.WriteLine($"OneSignal Player ID: {playerId}");
        }

        private void OnNotificationClicked(object sender, NotificationClickedEventArgs e)
        {
            var data = e.Notification.AdditionalData;
            var hasFbChatIdKey = data.TryGetValue("chatId", out var fbChatIdObj);
            var hasSubscriptionExpiredKey = data.TryGetValue("isSubscriptionExpired", out var subscriptionExpiredObj);
            var messageKey = data.TryGetValue("message", out var message);

            if (data != null && hasFbChatIdKey && hasSubscriptionExpiredKey)
            {
                string fbChatId = fbChatIdObj!.ToString()!;
                bool isParsed = bool.TryParse(subscriptionExpiredObj!.ToString(), out bool isSubscriptionExpired);

                // Navigate to specific chat
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!isSubscriptionExpired)
                    {
                        Navigation.NavigateTo($"/chat?isNotification=true&fbChatId={fbChatId}");
                    }
                    else
                    {
                        Navigation.NavigateTo($"/packages?isExpired={isSubscriptionExpired}&message={message!.ToString()}");
                    }
                });
            }
        }

        private async Task<TableData<GetMyAccountsHttpResponse>> ServerReload(TableState state, CancellationToken token)
        {
            var response = await AccountService.GetMyAccounts<BaseResponse<List<GetMyAccountsHttpResponse>>>();
            int totalItems = 0;
            List<GetMyAccountsHttpResponse> data = new List<GetMyAccountsHttpResponse>();
            if (response.IsSuccess && response.Data is not null)
            {
                data = response.Data;
                totalItems = response.Data.Count;
            }
            else
            {
                Snackbar.Add(response?.Message??"Something went wrong when wrong while fetching your accounts details", Severity.Error);
            }

            table.Items = data;
            return new TableData<GetMyAccountsHttpResponse>() { TotalItems = totalItems, Items = data };
        }
        public async Task AddNewAccount()
        {
            var parameters = new DialogParameters();

            var result = await DialogService.Show<UpsertAccount>("", parameters).Result;

            if (!result.Canceled)
            {
                await table.ReloadServerData();
            }
        }

        public async Task EditAccount(int accountId, string Name, string Cookie)
        {
            var parameters = new DialogParameters();
            parameters.Add("AccountId", accountId);
            parameters.Add("Name", Name);
            parameters.Add("Cookie", Cookie);
            var result = await DialogService.Show<UpsertAccount>("", parameters).Result;
            if (!result.Canceled)
            {
                await table.ReloadServerData();
            }

        }

        public async Task ToggleAccountStatus(int accountId, bool isActive)
        {
            var parameters = new DialogParameters();
            var lockStatus = isActive ? "deaactivate" : "activate";
            parameters.Add("ContentText", $"Do you want to {lockStatus} this account?");
            parameters.Add("ButtonText", $"{lockStatus}");
            parameters.Add("Color", Color.Primary);

            var options = new DialogOptions() { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };

            var dialog = await DialogService.Show<ConfirmationDialog>($"Do you want to {lockStatus} this user?", parameters, options).Result;

            if (dialog.Canceled)
            {
                return;
            }

            var resposne = await AccountService.ToggleAccountStatus<BaseResponse<ToggleAccountStatusHttpResponse>>(accountId);

            if (resposne is not null &&  resposne.IsSuccess)
            {
                Snackbar.Add(resposne.Message, Severity.Success);
                await table.ReloadServerData();
            }
            else
            {
                Snackbar.Add(resposne?.Message ?? "Something went wrong, please try later", Severity.Error);
            }
        }
    }
}
