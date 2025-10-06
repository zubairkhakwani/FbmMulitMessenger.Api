using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services
{
    internal class SubscriptionService : ISubscriptionSerivce
    {
        private readonly IBaseService _baseService;

        public SubscriptionService(IBaseService baseService)
        {
            this._baseService=baseService;
        }
        public async Task<T> GetMySubscription<T>() where T : class
        {
            var apiRequest = new ApiRequest<object>()
            {
                ApiType = Utility.SD.ApiType.GET,
                Url ="api/subscription/me"

            };
            return await _baseService.SendAsync<object, T>(apiRequest);
        }
    }
}
