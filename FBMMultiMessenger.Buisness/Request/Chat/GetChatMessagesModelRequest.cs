using FBMMultiMessenger.Contracts.Response;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Chat
{
    public class GetChatMessagesModelRequest : IRequest<BaseResponse<List<GetChatMessagesModelResponse>>>
    {
        public int ChatId { get; set; }
        public int UserId { get; set; } // Current User Id
    }
    public class GetChatMessagesModelResponse
    {
        public string Message { get; set; } = null!;
        public bool IsReceived { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
