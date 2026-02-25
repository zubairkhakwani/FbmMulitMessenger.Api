using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Chat
{
    public class GetChatMessagesModelRequest : IRequest<BaseResponse<List<GetChatMessagesModelResponse>>>
    {
        public int ChatId { get; set; }
    }
    public class GetChatMessagesModelResponse
    {
        public int ChatMessageId { get; set; } // Primary key of this chat message table
        public int ChatId { get; set; } // Foreign key referencing the chat this message belongs to
        public string? FbMessageId { get; set; } // Facebook message Id
        public string? FbMessageReplyId { get; set; }
        public string Message { get; set; } = null!;
        public bool IsReceived { get; set; } // This will tell if we send the message or we received the message
        public bool IsSent { get; set; } // This will tell whether message was send successfully to facebook via our app.
        public bool IsTextMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsAudioMessage { get; set; }

        public MessageReplyModelResponse? MessageReply { get; set; }

        public DateTime CreatedAt { get; set; }
        public long? FbTimeStamp { get; set; }
    }
    public class MessageReplyModelResponse
    {
        public MessageReplyType Type { get; set; }
        public string? Reply { get; set; }
        public string ReplyTo { get; set; } = string.Empty;
        public List<MessageReplyFileModelResponse>? Attachments { get; set; }
    }
    public class MessageReplyFileModelResponse
    {
        public string Url { get; set; } = string.Empty;
    }
}
