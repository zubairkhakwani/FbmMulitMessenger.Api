using FBMMultiMessenger.Contracts.Enums;

namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class UpdateAccountStatusHttpRequest
    {
        public int? ProxyId { get; set; }
        public List<AccountUpdateHttpOperation> Accounts { get; set; } = new();
    }

    public class AccountUpdateHttpOperation
    {
        public int AccountId { get; set; }
        public AccountConnectionStatus ConnectionStatus { get; set; }
        public AccountAuthStatus AuthStatus { get; set; }
        public AccountReason Reason { get; set; }
        public bool FreeServer { get; set; }
    }

    public class UpdateAccountStatusHttpResponse
    {
    }
}
