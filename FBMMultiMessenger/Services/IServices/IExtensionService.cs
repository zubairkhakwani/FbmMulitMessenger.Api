using FBMMultiMessenger.Contracts.Contracts.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IExtensionService
    {
        Task<T> Notify<T>(NotifyExtensionRequest httpRequest) where T : class;
    }
}
