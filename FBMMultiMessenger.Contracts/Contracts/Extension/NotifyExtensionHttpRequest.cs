using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Extension
{
    public class NotifyExtensionHttpRequest
    {
        [Required]
        public string FbChatId { get; set; } = null!;

        public string? Message { get; set; }
        public string OfflineUniqueId { get; set; } = string.Empty;

        public List<IFormFile>? Files { get; set; }
    }


    public class NotifyExtensionRequest
    {
        [Required]
        public string FbChatId { get; set; } = null!;


        [Required]
        public string Message { get; set; } = null!;

        public string OfflineUniqueId { get; set; } = string.Empty;

        public List<IBrowserFile>? Files { get; set; }

    }

    public class NotifyExtensionHttpResponse
    {
        public bool IsSubscriptionExpired { get; set; }

    }
}
