using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class DefaultMessage
    {
        public int Id { get; set; }
        public required string Message { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        /// <summary>
        /// When true, free accounts (and newly created ones) pick up this message.
        /// Accounts that already have a specific DefaultMessageId are never overwritten.
        /// </summary>
        public bool ApplyToUpcomingAccounts { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }


        //Navigation Properties
        public User User { get; set; } = null!;
        public List<Account> Accounts { get; set; } = new List<Account>();
    }
}
