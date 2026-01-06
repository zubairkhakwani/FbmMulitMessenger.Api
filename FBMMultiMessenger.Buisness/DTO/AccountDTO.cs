namespace FBMMultiMessenger.Buisness.DTO
{
    //This class is responsible for sending account details to user's local server.
    public class AccountDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Cookie { get; set; } = string.Empty;

        public string? DefaultMessage { get; set; }
        public bool IsCookieChanged { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
