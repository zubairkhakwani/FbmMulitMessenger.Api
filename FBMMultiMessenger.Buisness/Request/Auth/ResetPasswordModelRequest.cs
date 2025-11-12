using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Auth
{
    public class ResetPasswordModelRequest : IRequest<BaseResponse<object>>
    {
        public string NewPassword { get; set; } = string.Empty;

        public string ConfirmPassword { get; set; } = string.Empty;

        public string Otp { get; set; } = string.Empty;
    }
}
