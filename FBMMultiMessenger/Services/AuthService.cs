using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;
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

        public AuthService(IBaseService baseService)
        {
            this._baseService=baseService;

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
