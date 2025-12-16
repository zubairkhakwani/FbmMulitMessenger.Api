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
        public string AuthStatus { get; set; } = string.Empty;
        public string ConnectionStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public AccountProxyHttpResponse? Proxy { get; set; }
    }

    public class AccountProxyHttpResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Ip_Port { get; set; } = string.Empty;
    }
}
