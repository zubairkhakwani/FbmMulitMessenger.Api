using FBMMultiMessenger.Contracts.Enums;

namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class UpdateAccountStatusHttpRequest
    {
        public List<AccountUpdateHttpOperation> Accounts { get; set; } = new();
    }

    public class AccountUpdateHttpOperation
    {
        public int AccountId { get; set; }
        public AccountStatus Status { get; set; }
        public bool FreeServer { get; set; }
    }

    public class UpdateAccountStatusHttpResponse
    {
    }
}
