using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class SendChatMessageHttpRequest
    {
        [Required]
        public int? ChatId { get; set; }


        [Required]
        public string Message { get; set; } = null!;
    }

    public class SendChatMessageHttpResponse
    {

    }
}
