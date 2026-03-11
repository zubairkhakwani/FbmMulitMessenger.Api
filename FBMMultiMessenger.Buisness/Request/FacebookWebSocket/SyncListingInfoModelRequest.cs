using MediatR;

namespace FBMMultiMessenger.Buisness.Request.FacebookWebSocket
{
    public class SyncListingInfoModelRequest : IRequest
    {
        public string? FbListingTitle { get; set; }
        public string? FbListingImg { get; set; }
        public string? FbListingId { get; set; }
        public string? UserProfileImg { get; set; }
        public int ChatId { get; set; }
    }
}
