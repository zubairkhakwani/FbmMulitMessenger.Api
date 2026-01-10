namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class SyncInitialMessagesHttpRequest
    {
        public string FbAccountId { get; set; }
        public int AccountId { get; set; }
        public List<SyncChats> Chats { get; set; }
    }

    public class SyncChats
    {
        public string FbChatId { get; set; }
        public string? OtherUserId { get; set; }
        public string? OtherUserName { get; set; }
        public string? OtherUserProfilePicture { get; set; }
        public string? ListingTitle { get; set; }
        public string? ListingImage { get; set; }
        public List<SyncMessages> Messages { get; set; }

        //these three are not coming yet.
        public string? FbListingId { get; set; }
        public string? FbListingLocation { get; set; }
        public decimal? FbListingPrice { get; set; }
    }

    public class SyncMessages
    {
        public string MessageId { get; set; }
        public string? Text { get; set; }
        public long Timestamp { get; set; }
        public bool IsReceived { get; set; }
        public bool IsTextMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsAudioMessage { get; set; }

        public List<string> Attachments { get; set; }
    }

    public class SyncInitialMessagesHttpResponse
    {

    }
}
