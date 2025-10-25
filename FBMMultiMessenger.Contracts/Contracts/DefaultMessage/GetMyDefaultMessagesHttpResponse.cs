using FBMMultiMessenger.Contracts.Contracts.Account;

namespace FBMMultiMessenger.Contracts.Contracts.DefaultMessage
{
    public class GetMyDefaultMessagesHttpResponse
    {
        public List<DefaultMessagesHttpResponse> DefaultMessages { get; set; } = new List<DefaultMessagesHttpResponse>();
        public List<GetMyAccountsHttpResponse> AllAccounts { get; set; } = new List<GetMyAccountsHttpResponse>();
    }

    public class DefaultMessagesHttpResponse
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<GetMyAccountsHttpResponse> Accounts { get; set; } = new List<GetMyAccountsHttpResponse>();
    }
}
