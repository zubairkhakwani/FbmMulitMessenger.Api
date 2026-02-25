using FBMMultiMessenger.Contracts.Enums;

namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class GetChatMessagesHttpResponse
    {
        public int ChatMessageId { get; set; } // Primary key of this chat message table
        public int ChatId { get; set; } // Foreign key referencing the chat this message belongs to
        public string? FbMessageId { get; set; } // Facebook message Id
        public string? FbMessageReplyId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsReceived { get; set; }
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }
        public bool IsSent { get; set; }
        public string UniqueId { get; set; } = string.Empty;
        public bool Sending { get; set; }
        public MessageReplyHttpResponse? MessageReply { get; set; }
        public DateTime CreatedAt { get; set; }
        public long? FbTimeStamp { get; set; }
    }
    public class MessageReplyHttpResponse
    {
        public MessageReplyType Type { get; set; }
        public string? Reply { get; set; }
        public string ReplyTo { get; set; } = string.Empty;
        public List<MessageReplyFileHttpResponse>? Attachments { get; set; }
    }
    public class MessageReplyFileHttpResponse
    {
        public string Url { get; set; } = string.Empty;
    }
}
