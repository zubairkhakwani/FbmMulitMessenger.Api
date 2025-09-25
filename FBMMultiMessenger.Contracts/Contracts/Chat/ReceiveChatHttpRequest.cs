using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class ReceiveChatHttpRequest
    {

        [Required]
        public int? AccountId { get; set; }

        [Required]
        public string? FbUserId { get; set; }

        [Required]
        public string? FbChatId { get; set; }

        [Required]
        public string? FbAccountId { get; set; }

        [Required]
        public string? FbListingId { get; set; }

        [Required]
        public string? FbListingTitle { get; set; }

        [Required]
        public string? FbListingLocation { get; set; }

        [Required]
        public decimal? FbListingPrice { get; set; }

        [Required]
        public string Message { get; set; } = null!;

        public bool IsReceived { get; set; }
    }

    public class ReceiveChatHttpResponse
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

        public DateTime StartedAt { get; set; }

    }
}
