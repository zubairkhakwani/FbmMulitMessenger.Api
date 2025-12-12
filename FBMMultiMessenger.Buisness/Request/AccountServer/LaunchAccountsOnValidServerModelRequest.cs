using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.AccountServer
{
    public class LaunchAccountsOnValidServerModelRequest : IRequest<BaseResponse<LaunchAccountsOnValidServerModelResponse>>
    {
        public List<Data.Database.DbModels.Account> AccountsToLaunch { get; set; } = new List<Data.Database.DbModels.Account>();
    }
    public class LaunchAccountsOnValidServerModelResponse
    {
        public List<int> SuccessfulAccountIds { get; set; } = new();
        public List<FailedAccountInfo> FailedAccounts { get; set; } = new();
    }

    public class FailedAccountInfo
    {
        public int AccountId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

}
