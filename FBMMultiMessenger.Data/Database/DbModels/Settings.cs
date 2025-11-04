namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class Settings
    {
        public int Id { get; set; }
        public string Extension_Version { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
