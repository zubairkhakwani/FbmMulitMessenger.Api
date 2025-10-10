using FBMMultiMessenger.Contracts.Response;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Chat
{
    public class GetChatMessagesModelRequest : IRequest<BaseResponse<List<GetChatMessagesModelResponse>>>
    {
        public int UserId { get; set; } // Current User Id
        public string FbChatId { get; set; } = null!;
    }
    public class GetChatMessagesModelResponse
    {
        public string FbChatId { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsReceived { get; set; } // This will tell if we send the message or we received the message
        public bool IsSent { get; set; } // This will tell whether message was send successfully to facebook via our app.
        public bool IsTextMessage { get; set; }
        public bool IsImageMessage { get; set; }
        public bool IsVideoMessage { get; set; }
        public bool IsAudioMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
