using FBMMultiMessenger.Contracts.Response;

namespace FBMMultiMessenger.Contracts.Contracts.Proxy
{
    public class GetMyProxiesHttpRequest : PageableRequest
    {

    }
    public class GetMyProxiesHttpResponse
    {
        public int Id { get; set; }

        public required string Ip_Port { get; set; }
        public required string Name { get; set; }

        public required string Password { get; set; }
        public bool IsActives { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
