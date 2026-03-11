using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Models.SignalR.App
{
    public class ChatInfoUpdatedSignalRModel
    {
        public int ChatId { get; set; }
        public string? FbListingId { get; set; } = null!;
        public string? FbListingTitle { get; set; }
        public string? FbListingImage { get; set; }
        public string? FbListingLocation { get; set; }
        public decimal? FbListingPrice { get; set; }
        public string? OtherUserId { get; set; }
        public string? OtherUserName { get; set; }
        public string? OtherUserProfilePicture { get; set; }
    }
}
