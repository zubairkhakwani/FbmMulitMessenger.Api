using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Profile
{
    public class GetMyProfileModelRequest : PageableRequest, IRequest<BaseResponse<GetMyProfileModelResponse>>
    {
    }
    public class GetMyProfileModelResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;

        public string StartedAt { get; set; } = string.Empty;
        public string ExpiredAt { get; set; } = string.Empty;
        public bool HasActiveSubscription { get; set; }
        public string RemainingTimeText { get; set; } = string.Empty;
        public int RemainingDaysCount { get; set; }

        public bool IsCurrentTrialSubscription { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
