using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class GetAllMyAccountsChatsHttpResponse
    {
        public List<GetMyChatsHttpResponse> Chats { get; set; } = new List<GetMyChatsHttpResponse>();
    }

    public class GetMyChatsHttpResponse
    {
        public int Id { get; set; }
        public string FbChatId { get; set; } = null!;
        public string FbListingTitle { get; set; } = null!;
        public string FbListingLocation { get; set; } = null!;
        public decimal? FbListingPrice { get; set; }
        public string? FbListingImage { get; set; }
        public string? UserProfileImage { get; set; }
        public string? MessagePreview { get; set; }
        public string? SenderName { get; set; } // You or Hadia.
        public bool IsRead { get; set; }
        public int UnReadCount { get; set; }

        public DateTime StartedAt { get; set; }
    }
}
