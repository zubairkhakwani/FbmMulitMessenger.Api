using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Contracts.Contracts.Subscription;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Notification;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OneSignalSDK.DotNet;

namespace FBMMultiMessenger.Components.Pages.Auth
{
    public partial class Login
    {
        [SupplyParameterFromForm]
        public LoginHttpRequest RequestModel { get; set; } = new LoginHttpRequest() { Email="", Password="" };

        [Inject]
        public NavigationManager navManager { get; set; }

        [Inject]
        public IAuthService AuthService { get; set; }


        [Inject]
        private NavigationManager Navigation { get; set; }

        [Inject]
        private ITokenProvider TokenProvider { get; set; }

        [Inject]
        AuthenticationStateProvider AuthenticationStateProvider { get; set; }


        [Inject]
        public ISubscriptionSerivce SubscriptionSerivce { get; set; }

        [Inject]
        private OneSignalService OneSignalService { get; set; }


        public string? ResponseError;
        private bool ShowLoader = false;


        public async Task OnValidPost()
        {
            ShowLoader = true;
            var response = await AuthService.LoginAsync<BaseResponse<LoginHttpResponse>>(RequestModel);

            if (response.Data is not null &&  !string.IsNullOrWhiteSpace(response.Data.Token))
            {
                await TokenProvider.SetTokenAsync(response.Data.Token);
                ((CustomAuthenticationStateProvider)AuthenticationStateProvider).NotifyStateChanged();

                //Tell OneSignal this device now belongs to this user
                if (DeviceInfo.Platform != DevicePlatform.WinUI)
                {
                    // Running on Android (either emulator or physical device)
                    OneSignalService.Login(response.Data.UserId.ToString());
                }
            }

            if (response.IsSuccess)
            {
                navManager.NavigateTo("/Chat");
                return;
            }

            if (!response.IsSuccess && response.RedirectToPackages)
            {
                var isSubscriptionExpired = response.Data?.IsSubscriptionExpired ?? false;

                Navigation.NavigateTo($"/packages?isExpired={isSubscriptionExpired}&message={response.Message}");
                return;
            }

            ShowLoader = false;
            ResponseError = response.Message;
        }
    }
}
