using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FBMMultiMessenger.Components.Pages.Auth
{
    public partial class Register 
    {
        //[SupplyParameterFromForm]
        public RegisterHttpRequest RequestModel { get; set; } = new();

        [Inject]
        public IAuthService AuthService { get; set; }

        [Inject]
        public NavigationManager navManager { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        public string? ResponseError { get; set; }

        [Inject]
        public ITokenProvider TokenProvider { get; set; }


        public async Task OnValidSubmit()
        {
            var registerResponse = await AuthService.RegisterAsync<BaseResponse<RegisterHttpResponse>>(RequestModel);

            if (registerResponse.IsSuccess)
            {
                var loginRequest = new LoginHttpRequest()
                {
                    Email = RequestModel.Email,
                    Password = RequestModel.Password
                };

                var loginResponse = await AuthService.LoginAsync<BaseResponse<LoginHttpResponse>>(loginRequest);
                if (loginResponse.IsSuccess && loginResponse.Data is not null && loginResponse.Data.Token is not null)
                {
                    await TokenProvider.SetTokenAsync(loginResponse.Data.Token);

                    var message = Uri.EscapeDataString("Registration complete — welcome aboard!");
                    navManager.NavigateTo($"/Account?message={message}");
                    return;
                }

                Snackbar.Add(!string.IsNullOrWhiteSpace(loginResponse.Message) ? loginResponse.Message : "Something went wrong while letting you in.", Severity.Error);
            }

            ResponseError =  registerResponse.Message;
        }
    }
}
