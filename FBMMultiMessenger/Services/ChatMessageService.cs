using FBMMultiMessenger.Contracts.Contracts.Chat;
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
    internal class ChatMessageService : IChatMessagesService
    {
        private readonly IBaseService _baseService;
        private readonly string _baseUrl;

        public ChatMessageService(IBaseService baseService, IConfiguration configuration)
        {
            this._baseService=baseService;
            this._baseUrl = configuration.GetValue<string>("Urls:BaseUrl")!;

        }
        public async Task<T> GetChatMessages<T>(int chatId) where T : class
        {
            var request = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url = _baseUrl+$"api/chat/{chatId}/chatmessages",
                Data = null
            };

            return await _baseService.SendAsync<object, T>(request);
        }

        public async Task<T> SendChatMessage<T>(SendChatMessageHttpRequest httpRequest) where T : class
        {
            var request = new ApiRequest<SendChatMessageHttpRequest>()
            {
                ApiType = SD.ApiType.POST,
                Url = _baseUrl+"api/chat/send",
                Data = httpRequest
            };

            return await _baseService.SendAsync<SendChatMessageHttpRequest, T>(request);
        }
    }
}
