using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.LocalServer
{
    public class LocalServerReconnectionModelRequest : IRequest<BaseResponse<LocalServerReconnectionModelResponse>>
    {
        public required int UserId { get; set; }
        public int AccountId { get; set; }
        public string UniqueId { get; set; } = string.Empty;
    }

    public class LocalServerReconnectionModelResponse { }

}
