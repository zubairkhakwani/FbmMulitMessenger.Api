using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class Proxy
    {
        public int Id { get; set; }

        [ForeignKey(nameof(UserId))]
        public int UserId { get; set; }

        public required string Ip_Port { get; set; }
        public required string Name { get; set; }

        public required string Password { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        //Navigation Properties
        public User User { get; set; } = null!;
        public List<Account> Accounts { get; set; } = new List<Account>();
    }
}
