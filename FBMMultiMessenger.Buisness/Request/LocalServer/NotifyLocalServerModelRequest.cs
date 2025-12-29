using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FBMMultiMessenger.Buisness.Request.LocalServer
{
    public class NotifyLocalServerModelRequest : IRequest<BaseResponse<NotifyLocalServerModelResponse>>
    {
        public string FbChatId { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string OfflineUniqueId { get; set; } = string.Empty;

        public List<IFormFile>? Files { get; set; }
    }

    public class NotifyLocalServerModelResponse
    {
        public bool IsSubscriptionExpired { get; set; }
        public string OfflineUniqueId { get; set; } = string.Empty;
    }
}
