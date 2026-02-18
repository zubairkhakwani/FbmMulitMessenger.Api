using FBMMultiMessenger.Contracts.Enums;

namespace FBMMultiMessenger.Buisness.Models.SignalR.App
{
    public class UserAccountSignalRModel
    {
        public int AppId { get; set; }
        public List<AccountStatusSignalRModel> AccountsStatus { get; set; } = new List<AccountStatusSignalRModel>();
    }


    public class AccountStatusSignalRModel
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = string.Empty;

        public AccountConnectionStatus ConnectionStatus { get; set; }
        public string ConnectionStatusText { get; set; } = string.Empty;

        public AccountAuthStatus AuthStatus { get; set; }
        public string AuthStatusText { get; set; } = string.Empty;

        public AccountLogOutReason LogoutReason { get; set; }
        public string LogoutReasonText { get; set; } = string.Empty;

        public bool IsConnected { get; set; }
    }
}
