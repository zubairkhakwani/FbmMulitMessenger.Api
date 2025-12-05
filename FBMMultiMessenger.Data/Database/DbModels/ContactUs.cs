using FBMMultiMessenger.Contracts.Enums;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class ContactUs
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public ContactSubject Subject { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public bool IsReplied { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime RepliedAt { get; set; }

    }
}
