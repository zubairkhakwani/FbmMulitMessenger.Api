using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Components.Pages.Authenticaion
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
        public IJSRuntime JS { get; set; }

        [Inject]
        private NavigationManager Navigation { get; set; }
        [Inject]
        private ITokenProvider TokenProvider { get; set; }

        public string? ResponseError;

        protected override async Task OnInitializedAsync()
        {
            string? token = await TokenProvider.GetTokenAsync();

            if (!string.IsNullOrWhiteSpace(token))
            {
                Navigation.NavigateTo("/Account");
            }
        }

        public async Task OnValidPost()
        {
            var request = await AuthService.LoginAsync<BaseResponse<LoginHttpResponse>>(RequestModel);

            if (request.IsSuccess)
            {
                await JS.InvokeVoidAsync("myInterop.setItem", SD.AccessToken, request.Data!.Token);
                navManager.NavigateTo("/Account", true);
                return;
            }

            ResponseError = request.Message;
        }
    }
}
