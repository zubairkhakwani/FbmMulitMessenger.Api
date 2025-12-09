using FBMMultiMessenger.Data.Database.DbModels;

namespace FBMMultiMessenger.Buisness.Models
{
    public class GenerateJWTModel
    {
        public int UserId { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }

        public required string Key { get; set; }
    }
}
