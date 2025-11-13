using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Auth
{
    public class ForgotPasswordModelRequest : IRequest<BaseResponse<object>>
    {
        public string Email { get; set; } = null!;
    }
}
