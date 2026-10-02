using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    [Index(nameof(FBChatId), nameof(FbAccountId), nameof(UserId), IsUnique = true)]
    public class Chat
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Account))]
        public int? AccountId { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }


        public string? FbUserId { get; set; }
        public string? FbAccountId { get; set; }
        public string? FBChatId { get; set; }
        public string? FbListingId { get; set; }
        public string? FbListingTitle { get; set; }
        public string? FbListingLocation { get; set; }
        public string? FBListingImage { get; set; }
        public string? UserProfileImage { get; set; }
        public string? OtherUserName { get; set; }
        public string? OtherUserId { get; set; }

        [Precision(18, 2)]
        public decimal? FbListingPrice { get; set; }

        public bool IsRead { get; set; }

        /// <summary>When true, chat stays at top of the user's list (ordered by PinOrder).</summary>
        public bool IsPinned { get; set; }

        /// <summary>When true, chat appears in the Favorites filter (ordered by FavoriteOrder).</summary>
        public bool IsFavorite { get; set; }

        /// <summary>Lower = higher among pinned chats. Null when not pinned.</summary>
        public int? PinOrder { get; set; }

        /// <summary>Lower = higher among favorite chats. Null when not favorited.</summary>
        public int? FavoriteOrder { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime UpdatedAt { get; set; }


        //Navigation Properties
        public Account? Account { get; set; }
        public User User { get; set; } = null!;
        public List<ChatMessages> ChatMessages { get; set; } = new List<ChatMessages>();
    }
}
