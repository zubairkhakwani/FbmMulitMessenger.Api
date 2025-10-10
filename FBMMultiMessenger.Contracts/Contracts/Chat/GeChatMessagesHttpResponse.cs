using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class GeChatMessagesHttpResponse
    {
        public string FBChatId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
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
