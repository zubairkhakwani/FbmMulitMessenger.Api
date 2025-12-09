using FBMMultiMessenger.Contracts.Enums;

namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class UpdateAccountStatusHttpRequest
    {
        public Dictionary<int, AccountStatus> AccountStatus { get; set; } = new Dictionary<int, AccountStatus>();
    }

    public class UpdateAccountStatusHttpResponse
    {
    }
}
