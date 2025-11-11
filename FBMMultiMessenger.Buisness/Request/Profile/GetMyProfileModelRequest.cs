using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Profile
{
    public class GetMyProfileModelRequest : IRequest<BaseResponse<GetMyProfileModelResponse>>
    {
    }
    public class GetMyProfileModelResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }
}
