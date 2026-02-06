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
        public string? MessageReply { get; set; }
        public string? MessageReplyTo { get; set; }

        public bool IsReceived { get; set; } // This will tell if we send the message or we received the message
        public bool IsSent { get; set; } // This will tell whether message was send successfully to facebook via our app.
        public bool IsTextMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsAudioMessage { get; set; }


        public DateTime CreatedAt { get; set; }
    }


}
