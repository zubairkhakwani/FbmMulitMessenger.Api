using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class ApiKey
    {
        public int Id { get; set; }

        [ForeignKey(nameof(UserId))]
        public int UserId { get; set; }

        public required string Key { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Reserved for key based authentication, not written by the key management endpoints.
        public DateTime? LastUsedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        //Navigation Properties
        public User User { get; set; } = null!;
    }
}
