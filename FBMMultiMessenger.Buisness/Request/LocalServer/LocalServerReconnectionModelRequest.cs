using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.LocalServer
{
    public class LocalServerReconnectionModelRequest : IRequest<BaseResponse<LocalServerReconnectionModelResponse>>
    {
        public string UniqueId { get; set; } = string.Empty;
    }

    public class LocalServerReconnectionModelResponse { }

}
