using FBMMultiMessenger.Contracts.Response;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Profile
{
    public class EditProfileModelRequest : IRequest<BaseResponse<object>>
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;
    }
}
