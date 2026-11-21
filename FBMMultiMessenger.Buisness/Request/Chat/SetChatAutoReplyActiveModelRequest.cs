using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Chat
{
    public class SetChatAutoReplyActiveModelRequest : IRequest<BaseResponse<SetChatAutoReplyActiveModelResponse>>
    {
        public int ChatId { get; set; }
        public bool IsAutoReplyActive { get; set; }
    }

    public class SetChatAutoReplyActiveModelResponse
    {
        public int ChatId { get; set; }
        public bool IsAutoReplyActive { get; set; }
    }
}
