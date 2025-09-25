using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Chat
{
    public class GeChatMessagesHttpResponse
    {
        public string Message { get; set; } = null!;
        public bool IsReceived { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
