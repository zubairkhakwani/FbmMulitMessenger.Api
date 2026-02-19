using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class UpdateAccountStatusModelRequest : IRequest<BaseResponse<UpdateAccountStatusModelResponse>>
    {
        public List<AccountUpdateModelOperation> Accounts { get; set; } = new();
    }

    public class AccountUpdateModelOperation
    {
        public int AccountId { get; set; }
        public AccountConnectionStatus ConnectionStatus { get; set; }
        public AccountAuthStatus AuthStatus { get; set; }
        public AccountReason Reason { get; set; }
        public bool FreeServer { get; set; }
    }

    public class UpdateAccountStatusModelResponse
    {
    }
}
