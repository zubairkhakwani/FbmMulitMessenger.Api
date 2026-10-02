using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class UpdateAccountCustomNameModelRequest : IRequest<BaseResponse<UpdateAccountCustomNameModelResponse>>
    {
        public int AccountId { get; set; }
        public string CustomName { get; set; } = string.Empty;
    }

    public class UpdateAccountCustomNameModelResponse
    {
        public int AccountId { get; set; }
        public string CustomName { get; set; } = string.Empty;
    }
}
