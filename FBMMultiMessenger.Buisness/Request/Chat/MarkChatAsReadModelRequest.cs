using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Chat
{
    public class MarkChatAsReadModelRequest : IRequest<BaseResponse<MarkChatAsReadModelResponse>>
    {
        public int ChatId { get; set; }
        public int LastLocalMessageId { get; set; }
    }

    public class MarkChatAsReadModelResponse
    {
        public List<int> MessagesIdsMarkedAsRead { get; set; }
    }
}
