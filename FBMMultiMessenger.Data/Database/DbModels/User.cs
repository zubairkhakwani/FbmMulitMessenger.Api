using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class User
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Role))]
        public int RoleId { get; set; }

        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string ContactNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailVerified { get; set; }

        public DateTime CreatedAt { get; set; }


        //Navigation Property
        public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public List<Account> Accounts { get; set; } = new List<Account>();
        public List<DefaultMessage> DefaultMessages { get; set; } = new List<DefaultMessage>();
        public List<VerificationToken> VerificationTokens { get; set; } = new List<VerificationToken>();
        public Role Role { get; set; } = null!;

    }
}
