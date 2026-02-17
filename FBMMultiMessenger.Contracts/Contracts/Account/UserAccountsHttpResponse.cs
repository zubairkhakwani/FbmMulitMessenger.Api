using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class GetMyAccountsHttpRequest : PageableRequest
    {
        public string? SelectedAuthStatus { get; set; }

    }

    public class UserAccountsOverviewHttpResponse
    {
        public PageableResponse<UserAccountsHttpResponse> UserAccounts { get; set; } = null!;
        public int ConnectedAccounts { get; set; }
        public int NotConnectedAccounts { get; set; }
    }
    public class UserAccountsHttpResponse
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
