using FBMMultiMessenger.Contracts.Shared;
using MediatR;

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
        public required string Name { get; set; }
        public required string Cookie { get; set; }
        public string? DefaultMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
