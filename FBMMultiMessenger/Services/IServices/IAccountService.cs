using FBMMultiMessenger.Contracts.Contracts.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Services.IServices
{
    public interface IAccountService
    {
        Task<T> UpsertAccountAsync<T>(UpsertAccountHttpRequest httpRequest, int? accountId) where T : class;

        Task<T> GetMyAccounts<T>() where T : class;

        Task<T> ToggleAccountStatus<T>(int accountId) where T : class;


        Task<T> GetMyChats<T>() where T : class;
    }
}
