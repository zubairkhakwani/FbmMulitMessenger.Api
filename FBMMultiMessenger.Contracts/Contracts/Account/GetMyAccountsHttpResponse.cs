using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class GetMyAccountsHttpRequest : PageableRequest
    {
    }
    public class GetMyAccountsHttpResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Cookie { get; set; }
        public string? DefaultMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
