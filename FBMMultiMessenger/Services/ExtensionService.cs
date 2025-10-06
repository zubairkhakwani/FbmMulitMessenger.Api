using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Request;
using FBMMultiMessenger.Services.IServices;
using FBMMultiMessenger.Utility;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FBMMultiMessenger.Utility.SD;

namespace FBMMultiMessenger.Services
{
    internal class ExtensionService : IExtensionService
    {
        private readonly IBaseService _baseService;

        public ExtensionService(IBaseService baseService)
        {
            this._baseService=baseService;
        }
        public async Task<T> Notify<T>(NotifyExtensionRequest httpRequest) where T : class
        {
            var request = new ApiRequest<NotifyExtensionRequest>()
            {
                ApiType = SD.ApiType.POST,
                Url ="api/extension/notify",
                Data = httpRequest,
                ContentType = ContentType.MultipartFormData
            };

            return await _baseService.SendAsync<NotifyExtensionRequest, T>(request);

        }
    }
}
