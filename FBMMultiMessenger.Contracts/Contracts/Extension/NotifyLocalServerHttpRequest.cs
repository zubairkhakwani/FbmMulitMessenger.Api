using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Extension
{
    public class NotifyLocalServerHttpRequest
    {
        [Required]
        public int ChatId { get; set; }
        public string? Message { get; set; }
        public string OfflineUniqueId { get; set; } = string.Empty;
        public string? FbMessageReplyId { get; set; }
        public List<IFormFile>? Files { get; set; }
    }

    public class NotifyLocalServerHttpResponse
    {
        public bool IsSubscriptionExpired { get; set; }
        public string? OfflineUniqueId { get; set; }
    }
}
