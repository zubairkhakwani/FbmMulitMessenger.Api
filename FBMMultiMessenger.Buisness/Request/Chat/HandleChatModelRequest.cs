using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Chat
{
    public class HandleChatModelRequest : IRequest<BaseResponse<HandleChatModelResponse>>
    {
        public required string FbChatId { get; set; }
        public int AccountId { get; set; }
        public required string FbAccountId { get; set; }
        public string? OtherUserId { get; set; }
        public string? OtherUserName { get; set; }
        public string? OtherUserProfilePicture { get; set; }
        public string? FbListingId { get; set; }
        public string? FbListingTitle { get; set; }
        public string? FbListingImg { get; set; }
        public string? UserProfileImg { get; set; }
        public string? FbMessageReplyId { get; set; }
        public decimal? FbListingPrice { get; set; }
        public string? FbListingLocation { get; set; }
        public required List<string> Messages { get; set; }
        public string? OfflineUniqueId { get; set; }
        public long? FbOTID { get; set; }
        public string? FbMessageId { get; set; }
        public long? Timestamp { get; set; }
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }
        public bool IsReceived { get; set; }
    }

    public class HandleChatModelResponse
    {

    }
}
