using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class SendChatMessagesHttpRequest
    {
        //[Required]
        public string? FbChatId { get; set; }

        //[Required]
        public string? FbAccountId { get; set; }

        //[Required]
        public string? FbListingId { get; set; }

        //[Required]
        public string? FbListingTitle { get; set; }

        //[Required]
        public string? FbListingLocation { get; set; }

        //[Required]
        public decimal? FbListingPrice { get; set; }

        // [Required]
        public List<string>? Messages { get; set; } = new List<string>();

        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }
    }

    public class SendChatMessagesHttpResponse
    {
        public int ChatId { get; set; }
        public string FbUserId { get; set; } = null!;

        public string FbChatId { get; set; } = null!;

        public string FbListingId { get; set; } = null!;

        public string FbAccountId { get; set; } = null!;

        public string FbListingTitle { get; set; } = null!;

        public string FbListingLocation { get; set; } = null!;

        public decimal FbListingPrice { get; set; }

        public string Message { get; set; } = null!;

        public bool IsRead { get; set; }

        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }

        public DateTime StartedAt { get; set; }
    }

}
