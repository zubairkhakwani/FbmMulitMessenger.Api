using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    [Index(nameof(FBTimestamp))]
    public class ChatMessages
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Chat))]
        public int ChatId { get; set; }
        public string? FbMessageId { get; set; }

        public string? FbMessageReplyId { get; set; }

        public long? FBTimestamp { get; set; }

        public string Message { get; set; } = string.Empty;

        public bool IsReceived { get; set; } // if true then the message was recevied other wise message was sent.

        public bool IsRead { get; set; }

        public bool IsSent { get; set; } //If true then means the message was succesfully sent otherwise inform user that message failed.
        public bool IsTextMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsAudioMessage { get; set; }
        public DateTime CreatedAt { get; set; }

        //Navigation Properties
        public Chat Chat { get; set; } = null!;
    }
}
