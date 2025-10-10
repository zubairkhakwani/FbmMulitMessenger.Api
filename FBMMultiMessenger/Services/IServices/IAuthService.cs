using FBMMultiMessenger.Contracts.Contracts.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IAuthService
    {
        Task<T> LoginAsync<T>(LoginHttpRequest httpRequest) where T : class;
        Task<T> RegisterAsync<T>(RegisterHttpRequest httpRequest) where T : class;

        Task Logout();

    }
}
