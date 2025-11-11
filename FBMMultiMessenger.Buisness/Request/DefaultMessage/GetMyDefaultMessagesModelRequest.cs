using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.DefaultMessage;
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
        public List<GetMyAccountsModelResponse> AllAccounts { get; set; } = new List<GetMyAccountsModelResponse>();
    }

    public class DefaultMessagesModelResponse
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<GetMyAccountsModelResponse> Accounts { get; set; } = new List<GetMyAccountsModelResponse>();
    }
}
