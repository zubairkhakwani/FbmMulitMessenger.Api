using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class ConnectAccountModelRequest : IRequest<BaseResponse<object>>
    {
        public int AccountId { get; set; }
    }

}
