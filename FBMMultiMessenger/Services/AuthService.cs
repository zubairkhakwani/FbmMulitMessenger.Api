using FBMMultiMessenger.Contracts.Contracts;
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
        private readonly string _baseUrl;

        public AuthService(IBaseService baseService, IConfiguration configuration)
        {
            this._baseService=baseService;
            this._baseUrl = configuration.GetValue<string>("Urls:BaseUrl")!;
        }
        public async Task<T> LoginAsync<T>(LoginHttpRequest httpRequest) where T : class
        {
            var apiRequest = new ApiRequest<LoginHttpRequest>()
            {
                ApiType = SD.ApiType.POST,
                Url = "https://localhost:7095/"+"api/auth/login",
                Data = httpRequest
            };

            return await _baseService.SendAsync<LoginHttpRequest, T>(apiRequest);
        }
    }
}
