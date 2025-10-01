using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Contracts.Contracts.Subscription;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;
using MediatR;
using MediatR.Pipeline;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public ISubscriptionSerivce SubscriptionSerivce { get; set; }

        public string? ResponseError;

        protected override async Task OnInitializedAsync()
        {
            string? token = await TokenProvider.GetTokenAsync();

            if (!string.IsNullOrWhiteSpace(token))
            {
                var response = await SubscriptionSerivce.GetMySubscription<BaseResponse<GetMySubscriptionHttpResponse>>();
                var isExpired = response.IsSubsriptionExpired;
                var isNotActive = !response.IsSubscriptionActive;

                if (isExpired || isNotActive)
                {
                    var message = Uri.EscapeDataString(response.Message);
                    Navigation.NavigateTo($"/packages?isExpired={isExpired}&message={message}");
                    return;
                }

                Navigation.NavigateTo("/Account");
            }
        }



        public async Task OnValidPost()
        {
            var response = await AuthService.LoginAsync<BaseResponse<LoginHttpResponse>>(RequestModel);

            if (response.IsSuccess)
            {
                await TokenProvider.SetTokenAsync(response!.Data!.Token!);
                var isExpired = response.IsSubsriptionExpired;
                var isNotActive = !response.IsSubscriptionActive;

                if (isExpired || isNotActive)
                {
                    var message = Uri.EscapeDataString(response.Message);
                    Navigation.NavigateTo($"/packages?isExpired={isExpired}&message={message}");
                    return;
                }

                navManager.NavigateTo("/Account");
                return;
            }

            ResponseError = response.Message;
        }
    }
}
