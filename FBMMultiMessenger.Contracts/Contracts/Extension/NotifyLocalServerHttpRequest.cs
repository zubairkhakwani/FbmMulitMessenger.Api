using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Extension
{
    public class NotifyLocalServerHttpRequest
    {
        [Required]
        public string FbChatId { get; set; } = null!;

        public string? Message { get; set; }
        public string OfflineUniqueId { get; set; } = string.Empty;

        public List<IFormFile>? Files { get; set; }
    }

    public class NotifyLocalServerHttpResponse
    {
        public bool IsSubscriptionExpired { get; set; }
        public string? OfflineUniqueId { get; set; }
    }
}
