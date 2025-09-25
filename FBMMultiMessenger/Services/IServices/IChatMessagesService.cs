using FBMMultiMessenger.Contracts.Contracts.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IChatMessagesService
    {
        Task<T> GetChatMessages<T>(int chatId) where T : class;
        Task<T> SendChatMessage<T>(SendChatMessageHttpRequest httpRequest) where T : class;
    }
}
