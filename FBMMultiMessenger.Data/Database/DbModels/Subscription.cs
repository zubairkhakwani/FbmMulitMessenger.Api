using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class Subscription
    {
        public int Id { get; set; }
        public int MaxLimit { get; set; }
        public int LimitUsed { get; set; }
        public bool IsActive { get; set; }
        public bool CanRunOnOurServer { get; set; } //tells whether this subscription can run browsers on our server or not.

        /// <summary>True when this subscription includes AI auto-reply.</summary>
        public bool HasAutoReply { get; set; }
        public int AutoReplyMaxLimit { get; set; }
        public int AutoReplyLimitUsed { get; set; }

        public bool IsTrial { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime ExpiredAt { get; set; }

        /// <summary>When an admin last changed this subscription (limit, expiry, or cancel).</summary>
        public DateTime? LastUpdatedAt { get; set; }

        [ForeignKey(nameof(LastUpdatedByUser))]
        public int? LastUpdatedByUserId { get; set; }

        /// <remarks>
        /// This property gets mapped to the PostgreSQL xmin system column for the sake of enabling concurrency on the given entity.
        /// </remarks>
        [Timestamp]
        public uint Version { get; set; }


        [ForeignKey(nameof(User))]
        public int UserId { get; set; }


        //Navigation Properties
        public User User { get; set; } = null!;
        public User? LastUpdatedByUser { get; set; }
    }
}
