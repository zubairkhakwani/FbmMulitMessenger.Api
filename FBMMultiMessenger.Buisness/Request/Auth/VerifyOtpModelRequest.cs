using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Auth
{
    public class VerifyOtpModelRequest : IRequest<BaseResponse<object>>
    {
        public required string Otp { get; set; }
    }
}
