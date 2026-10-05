using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    /// <summary>
    /// Browser FCM token registered by an admin on the Portal.
    /// </summary>
    public class AdminFcmToken
    {
        public int Id { get; set; }

        /// <summary>Optional Portal admin user id (informational).</summary>
        public int? UserId { get; set; }

        [MaxLength(2048)]
        public string Token { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
