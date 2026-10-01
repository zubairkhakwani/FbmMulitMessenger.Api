using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.ChatListPreference
{
    public class GetChatListPreferenceModelRequest : IRequest<BaseResponse<GetChatListPreferenceModelResponse>>
    {
    }

    public class GetChatListPreferenceModelResponse
    {
        public List<int> PinnedChatIds { get; set; } = new();
        public List<int> FavoriteChatIds { get; set; } = new();
    }

    public class UpsertChatListPreferenceModelRequest : IRequest<BaseResponse<GetChatListPreferenceModelResponse>>
    {
        public List<int> PinnedChatIds { get; set; } = new();
        public List<int> FavoriteChatIds { get; set; } = new();
    }
}
