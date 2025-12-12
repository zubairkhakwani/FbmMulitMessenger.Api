using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.LocalServer
{
    public class LocalServerHeartbeatModelRequest : IRequest<BaseResponse<LocalServerHeartbeatModelResponse>>
    {
        public string ServerId { get; set; } = string.Empty;
        public List<int> ActiveAccountIds { get; set; } = new List<int>(); // Accounts running on this server
    }

    public class LocalServerHeartbeatModelResponse
    {
    }
}
