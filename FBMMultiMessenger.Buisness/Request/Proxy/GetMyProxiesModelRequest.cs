using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Proxy
{
    public class GetMyProxiesModelRequest : PageableRequest, IRequest<BaseResponse<PageableResponse<GetMyProxiesModelResponse>>>
    {

    }
    public class GetMyProxiesModelResponse
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
