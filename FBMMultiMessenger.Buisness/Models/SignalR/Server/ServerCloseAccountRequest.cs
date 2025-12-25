namespace FBMMultiMessenger.Buisness.Models.SignalR.Server
{
    public class ServerCloseAccountRequest
    {
        public string ServerId { get; set; } = string.Empty; // Unique Id of the server 
        public List<AccountsCloseInfo> Accounts { get; set; } = new List<AccountsCloseInfo>();


    }

    public class AccountsCloseInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Cookie { get; set; } = string.Empty;
        public bool IsCookieChanged { get; set; }
        public bool IsBrowserOpenRequest { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
