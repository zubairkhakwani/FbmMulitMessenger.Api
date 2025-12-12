using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Proxy
{
    public class UpsertProxyModelRequest : IRequest<BaseResponse<UpsertProxyModelResponse>>
    {
        public int? ProxyId { get; set; }
        public int CurrentUserId { get; set; }
        public string Ip_Port { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpsertProxyModelResponse
    {

    }
}
