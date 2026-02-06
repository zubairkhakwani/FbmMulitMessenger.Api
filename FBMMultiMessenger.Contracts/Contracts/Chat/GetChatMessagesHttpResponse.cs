using Microsoft.AspNetCore.Components.Forms;

namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class GetChatMessagesHttpResponse
    {
        public int ChatMessageId { get; set; } // Primary key of this chat message table
        public int ChatId { get; set; } // Foreign key referencing the chat this message belongs to
        public string? FbMessageId { get; set; } // Facebook message Id
        public string? FbMessageReplyId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? MessageReply { get; set; }
        public string? MessageReplyTo { get; set; }
        public bool IsReceived { get; set; }
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }
        public bool IsSent { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UniqueId { get; set; } = string.Empty;
        public bool Sending { get; set; }
        public List<FileData> FileData { get; set; } = new();
    }

    public class FileData
    {
        public string Id { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public bool IsVideo { get; set; }

        public IBrowserFile File { get; set; } = null!;
    }
}
