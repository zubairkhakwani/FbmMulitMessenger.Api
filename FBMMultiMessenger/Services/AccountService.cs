using FBMMultiMessenger.Contracts.Contracts.Account;
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
    internal class AccountService : IAccountService
    {
        private readonly IBaseService _baseService;
        private readonly string _baseUrl;

        public AccountService(IBaseService baseService, IConfiguration configuration)
        {
            this._baseService=baseService;
            this._baseUrl = configuration.GetValue<string>("Urls:BaseUrl")!;

        }
        public async Task<T> UpsertAccountAsync<T>(UpsertAccountHttpRequest httpRequest, int? accountId) where T : class
        {
            var apiType = SD.ApiType.POST;
            var url = "api/account";

            if (accountId is not null)
            {
                apiType = SD.ApiType.PUT;
                url = $"api/account/{accountId}";
            }

            var request = new ApiRequest<UpsertAccountHttpRequest>()
            {
                ApiType = apiType,
                Url = _baseUrl+url,
                Data = httpRequest
            };

            return await _baseService.SendAsync<UpsertAccountHttpRequest, T>(request);
        }

        public async Task<T> GetMyAccounts<T>() where T : class
        {
            var request = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url = _baseUrl+"api/account/me",
                Data = null
            };
            return await _baseService.SendAsync<object, T>(request);
        }

        public async Task<T> ToggleAccountStatus<T>(int accountId) where T : class
        {
            var request = new ApiRequest<ToggleAccountStatusHttpRequest>()
            {
                ApiType = SD.ApiType.PUT,
                Url = _baseUrl+$"api/account/{accountId}/status",
                Data = null
            };

            return await _baseService.SendAsync<ToggleAccountStatusHttpRequest, T>(request);
        }
    }
}

