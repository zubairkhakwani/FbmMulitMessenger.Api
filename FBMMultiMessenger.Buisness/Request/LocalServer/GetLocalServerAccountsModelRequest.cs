using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Org.BouncyCastle.Asn1.Crmf;

namespace FBMMultiMessenger.Buisness.Request.LocalServer
{
    public class GetLocalServerAccountsModelRequest : IRequest<BaseResponse<List<GetLocalServerAccountsModelResponse>>>
    {
        public int Limit { get; set; }

        public string LocalServerId { get; set; } = null!;
    }

    public class GetLocalServerAccountsModelResponse
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public required string Name { get; set; }
        public required string Cookie { get; set; }
        public string? DefaultMessage { get; set; }
        public DateTime CreatedAt { get; set; }

        public LocalServerAccountsProxyModelResponse? Proxy { get; set; }
    }

    public class LocalServerAccountsProxyModelResponse
    {
        public int Id { get; set; }
        public string Ip_Port { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
