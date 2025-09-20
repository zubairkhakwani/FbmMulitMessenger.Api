using FBMMultiMessenger.Contracts.Contracts;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Services.IServices;
using MediatR;
using Microsoft.AspNetCore.Components;
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

        public string ResponseError;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        public async Task OnValidPost()
        {
            var request = await AuthService.LoginAsync<BaseResponse<LoginHttpResponse>>(RequestModel);

            if (request.IsSuccess)
            {
                navManager.NavigateTo("/");
            }
        }
    }
}
