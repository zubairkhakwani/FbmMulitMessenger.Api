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
        private readonly string _baseUrl;

        public SubscriptionService(IBaseService baseService, IConfiguration configuration)
        {
            this._baseService=baseService;
            this._baseUrl = configuration.GetValue<string>("Urls:BaseUrl")!;
        }
        public async Task<T> GetMySubscription<T>() where T : class
        {
            var apiRequest = new ApiRequest<object>()
            {
                ApiType = Utility.SD.ApiType.GET,
                Url =_baseUrl+"api/subscription/me"

            };
            return await _baseService.SendAsync<object, T>(apiRequest);
        }
    }
}
