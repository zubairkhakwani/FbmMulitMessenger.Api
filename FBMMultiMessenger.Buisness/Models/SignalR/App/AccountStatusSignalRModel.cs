namespace FBMMultiMessenger.Buisness.Models.SignalR.App
{
    public class UserAccountSignalRModel
    {
        public int UserId { get; set; }
        public List<AccountStatusSignalRModel> AccountsStatus { get; set; } = new List<AccountStatusSignalRModel>();
    }


    public class AccountStatusSignalRModel
    {
        public int AccountId { get; set; }

        public string AccountStatus { get; set; } = string.Empty;
    }
}
