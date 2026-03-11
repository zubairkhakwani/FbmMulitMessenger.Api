using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using System.Runtime;

namespace FBMMultiMessenger.Buisness.Request.LocalServer
{
    public class LocalServerDisconnectionModelRequest : IRequest<BaseResponse<LocalServerDisconnectionModelResponse>>
    {
        public required int UserId { get; set; }
        public int AccountId { get; set; }
        public string UniqueId { get; set; } = string.Empty;
    }
    public class LocalServerDisconnectionModelResponse
    {
    }
}
