using AutoMapper;
using FBMMultiMessenger.Buisness.Request.ChatListPreference;
using FBMMultiMessenger.Contracts.Contracts.ChatListPreference;
using FBMMultiMessenger.Contracts.Shared;

namespace FBMMultiMessenger.Buisness.Mapping.ChatListPreference
{
    public class ChatListPreferenceProfile : Profile
    {
        public ChatListPreferenceProfile()
        {
            CreateMap<GetChatListPreferenceModelResponse, GetChatListPreferenceHttpResponse>();
            CreateMap<BaseResponse<GetChatListPreferenceModelResponse>, BaseResponse<GetChatListPreferenceHttpResponse>>();

            CreateMap<UpsertChatListPreferenceHttpRequest, UpsertChatListPreferenceModelRequest>();
        }
    }
}
