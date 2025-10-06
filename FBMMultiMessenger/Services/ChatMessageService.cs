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

namespace FBMMultiMessenger.Services
{
    internal class ChatMessageService : IChatMessagesService
    {
        private readonly IBaseService _baseService;

        public ChatMessageService(IBaseService baseService)
        {
            this._baseService=baseService;
        }
        public async Task<T> GetChatMessages<T>(string fbChatId) where T : class
        {
            var request = new ApiRequest<object>()
            {
                ApiType = SD.ApiType.GET,
                Url = $"api/chat/{fbChatId}/chatmessages",
                Data = null
            };

            return await _baseService.SendAsync<object, T>(request);
        }
    }
}
