using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class HandleChatHttpRequest
    {

        //[Required]
        public string? FbChatId { get; set; }

        //[Required]
        public string? FbAccountId { get; set; }

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
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }

        public bool IsReceived { get; set; } //this bit will determine whether the message is received to user or the user has sent it.

    }

    public class HandleChatHttpResponse
    {
        public int ChatId { get; set; }
        public string Message { get; set; } = null!;
        public string FbUserId { get; set; } = null!;
        public string FbChatId { get; set; } = null!;
        public string? FbListingId { get; set; } = null!;
        public string FbAccountId { get; set; } = null!;
        public string? FbListingTitle { get; set; }
        public string? FbListingImage { get; set; }
        public string? FbListingLocation { get; set; }
        public decimal? FbListingPrice { get; set; }
        public string? UserProfileImage { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public string LastMessageFrom { get; set; } = string.Empty;

        public bool IsRead { get; set; }
        public bool IsTextMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsAudioMessage { get; set; }

        public bool IsReceived { get; set; }

        public DateTime StartedAt { get; set; }
    }
}
