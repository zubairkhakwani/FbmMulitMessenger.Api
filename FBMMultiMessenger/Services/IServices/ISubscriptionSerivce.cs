using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services.IServices
{
    public interface ISubscriptionSerivce
    {
        Task<T> GetMySubscription<T>() where T : class;
    }
}
