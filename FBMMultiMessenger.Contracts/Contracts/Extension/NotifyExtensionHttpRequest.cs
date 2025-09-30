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

        public List<IFormFile>? FIles { get; set; }
    }


    public class NotifyExtensionRequest
    {
        [Required]
        public string FbChatId { get; set; } = null!;


        [Required]
        public string Message { get; set; } = null!;

        public List<IBrowserFile>? FIles { get; set; }

    }

    public class NotifyExtensionHttpResponse
    {

    }
}
