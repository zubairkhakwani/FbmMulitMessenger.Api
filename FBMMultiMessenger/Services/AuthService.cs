using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Helpers;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services
{
    internal class AuthService : IAuthService
    {
        private readonly IBaseService _baseService;
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly ITokenProvider TokenProvider;

        public AuthService(IBaseService baseService, AuthenticationStateProvider authState, ITokenProvider tokenProvider)
        {
            this._baseService=baseService;
            this._authenticationStateProvider = authState;
            this.TokenProvider =tokenProvider;

        }
        public async Task<T> LoginAsync<T>(LoginHttpRequest httpRequest) where T : class
        {
            var apiRequest = new ApiRequest<LoginHttpRequest>()
            {
                ApiType = SD.ApiType.POST,
                Url ="api/auth/login",
                Data = httpRequest
            };

            return await _baseService.SendAsync<LoginHttpRequest, T>(apiRequest);
        }

        public async Task Logout()
        {
            await TokenProvider.RemoveTokenAsync();
            ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();
        }

        public async Task<T> RegisterAsync<T>(RegisterHttpRequest httpRequest) where T : class
        {
            var apiRequest = new ApiRequest<RegisterHttpRequest>()
            {
                ApiType  = SD.ApiType.POST,
                Url = "api/auth/register",
                Data  = httpRequest
            };

            return await _baseService.SendAsync<RegisterHttpRequest, T>(apiRequest);
        }
    }
}
