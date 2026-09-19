using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.DefaultMessage
{
    public class GetMyDefaultMessagesModelRequest : IRequest<BaseResponse<GetMyDefaultMessagesModelResponse>>
    {

    }
    public class GetMyDefaultMessagesModelResponse
    {
        public List<DefaultMessagesModelResponse> DefaultMessages = new List<DefaultMessagesModelResponse>();
        public List<UserAccountsModelResponse> AllAccounts { get; set; } = new List<UserAccountsModelResponse>();
    }

    public class DefaultMessagesModelResponse
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool ApplyToUpcomingAccounts { get; set; }
        public List<UserAccountsModelResponse> Accounts { get; set; } = new List<UserAccountsModelResponse>();
    }
}
