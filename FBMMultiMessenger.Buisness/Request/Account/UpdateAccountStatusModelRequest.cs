using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class UpdateAccountStatusModelRequest : IRequest<BaseResponse<UpdateAccountStatusModelResponse>>
    {
        public Dictionary<int, AccountStatus> AccountStatus { get; set; } = new Dictionary<int, AccountStatus>();
    }

    public class UpdateAccountStatusModelResponse
    {
    }
}
