using FBMMultiMessenger.Contracts.Response;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Request.Chat
{
    public class SendChatMessageModelRequest : IRequest<BaseResponse<SendChatMessageModelResponse>>
    {
        public int UserId { get; set; } // Current user id
        public int ChatId { get; set; }
        public string Message { get; set; } = null!;
    }

    public class SendChatMessageModelResponse
    {
    }
}
