using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Account
{
    public class GetExtensionIdentityModelRequest : IRequest<BaseResponse<GetExtensionIdentityModelResponse>>
    {
    }

    public class GetExtensionIdentityModelResponse
    {
        public int UserId { get; set; }
    }
}
