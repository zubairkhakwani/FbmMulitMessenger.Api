namespace FBMMultiMessenger.Buisness.DTO
{
    //This class is responsible for sending account details to our server that launches browser.
    public class AccountDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Cookie { get; set; } = string.Empty;
        public bool IsCookieChanged { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
