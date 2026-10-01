using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    /// <summary>
    /// Per-user pin/favorite flags for a chat. Independent of Chat.UpdatedAt / message sync.
    /// </summary>
    [Index(nameof(UserId), nameof(ChatId), IsUnique = true)]
    public class UserChatListPreference
    {
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        [ForeignKey(nameof(Chat))]
        public int ChatId { get; set; }

        public bool IsPinned { get; set; }
        public bool IsFavorite { get; set; }

        /// <summary>Lower = higher in list (0 = top). Null when not pinned.</summary>
        public int? PinOrder { get; set; }

        /// <summary>Lower = higher in list (0 = top). Null when not favorited.</summary>
        public int? FavoriteOrder { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User User { get; set; } = null!;
        public Chat Chat { get; set; } = null!;
    }
}
