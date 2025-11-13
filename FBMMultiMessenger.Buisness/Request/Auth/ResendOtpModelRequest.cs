using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Auth
{
    public class ResendOtpModelRequest : IRequest<BaseResponse<object>>
    {
        public required string Email { get; set; }
        public bool IsEmailVerification { get; set; }
    }
}
