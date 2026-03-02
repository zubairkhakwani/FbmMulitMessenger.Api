namespace FBMMultiMessenger.Contracts.Contracts.LocalServer
{
    public class GetLocalServerAccountsHttpResponse
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public required string Name { get; set; }
        public required string Cookie { get; set; }
        public string? DefaultMessage { get; set; }
        public DateTime CreatedAt { get; set; }

        public LocalServerAccountsProxyHttpResponse? Proxy { get; set; }
    }

    public class LocalServerAccountsProxyHttpResponse
    {
        public int Id { get; set; }
        public string Ip_Port { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
