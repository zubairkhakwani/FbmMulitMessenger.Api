namespace FBMMultiMessenger.Buisness.DTO
{
    //This class is responsible for sending account details to our server that launches browser.
    public class AccountDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Cookie { get; set; } = string.Empty;
        public bool IsCookieChanged { get; set; }
        public bool IsBrowserOpenRequest { get; set; }
        // Indicates that the user triggered multiple "Open Browser" actions rapidly,
        // which may lead to a race condition.

        public DateTime CreatedAt { get; set; }
    }
}
