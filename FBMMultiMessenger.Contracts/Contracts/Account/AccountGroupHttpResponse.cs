namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class UpsertAccountGroupHttpRequest
    {
        public string Name { get; set; } = string.Empty;
        public List<int> AccountIds { get; set; } = new();
    }

    public class AccountGroupHttpResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public List<int> AccountIds { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class GetMyAccountGroupsHttpResponse
    {
        public List<AccountGroupHttpResponse> Groups { get; set; } = new();
    }
}
