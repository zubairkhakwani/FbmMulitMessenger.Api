using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System.Runtime;

namespace FBMMultiMessenger.Buisness.Request.LocalServer
{
    public class HandleLocalServerDisconnectionModelRequest : IRequest<BaseResponse<HandleLocalServerDisconnectionModelResponse>>
    {
        public string LocalServerId { get; set; } = string.Empty;
    }
    public class HandleLocalServerDisconnectionModelResponse
    {
    }
}
