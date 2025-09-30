using FBMMultiMessenger.Contracts.Contracts.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IChatMessagesService
    {
        Task<T> GetChatMessages<T>(string fbChatId) where T : class;
    }
}
