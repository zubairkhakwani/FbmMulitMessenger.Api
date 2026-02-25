using FBMMultiMessenger.Contracts.Enums;

namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class HandleChatHttpRequest
    {

        //[Required]
        public string? FbChatId { get; set; }

        //[Required]
        public string? FbAccountId { get; set; }

        public int AccountId { get; set; }


        //[Required]
        public string? FbListingId { get; set; }

        //[Required]
        public string? FbListingTitle { get; set; }
        public string? FbListingImg { get; set; }
        public string? UserProfileImg { get; set; }


        //[Required]
        public string? FbListingLocation { get; set; }

        //[Required]
        public decimal? FbListingPrice { get; set; }

        // [Required]
        public List<string>? Messages { get; set; }
        public string? OfflineUniqueId { get; set; }
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }

        public bool IsReceived { get; set; } //this bit will determine whether the message is received to user or the user has sent it.
        public long? FbOTID { get; set; }
        public string? FbMessageId { get; set; }
        public string? FbMessageReplyId { get; set; }
        public long? Timestamp { get; set; }
    }

    public class HandleChatHttpResponse
    {
        public int ChatId { get; set; }
        public int ChatMessageId { get; set; }
        public string Message { get; set; } = null!;
        public string? FbMessageId { get; set; }
        public string? FbMessageReplyId { get; set; }
        public string? OfflineUniqueId { get; set; }
        public string FbUserId { get; set; } = null!;
        public string FbChatId { get; set; } = null!;
        public string? FbListingId { get; set; } = null!;
        public string FbAccountId { get; set; } = null!;
        public string? FbListingTitle { get; set; }
        public string? FbListingImage { get; set; }
        public string? FbListingLocation { get; set; }
        public decimal? FbListingPrice { get; set; }
        public string? UserProfileImage { get; set; }
        public string MessagPreview { get; set; } = string.Empty;
        public string MessagePreviewFrom { get; set; } = string.Empty;
        public bool IsRead { get; set; }

        //The actual message
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }
        public bool IsReceived { get; set; }

        //The reply message
        public MessageReplyHttpResponse? MessageReply { get; set; }

        public DateTime CreatedAt { get; set; }
        public long? FbTimestamp { get; set; }
    }
}
