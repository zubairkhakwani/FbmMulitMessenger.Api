using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Mapping.Chat
{
    public class ChatProfile : Profile
    {
        public ChatProfile()
        {
            CreateMap<HandleChatHttpRequest, HandleChatModelRequest>();

            CreateMap<HandleChatModelResponse, HandleChatHttpResponse>();
            CreateMap<BaseResponse<HandleChatModelResponse>, BaseResponse<HandleChatHttpResponse>>();

            CreateMap<SyncInitialMessagesHttpRequest, SyncInitialMessagesModelRequest>();
            CreateMap<SyncChats, SyncChatsModel>();
            CreateMap<SyncMessages, SyncMessagesModel>();

            CreateMap<SyncInitialMessagesModelResponse, SyncInitialMessagesHttpResponse>();
            CreateMap<BaseResponse<SyncInitialMessagesModelResponse>, BaseResponse<SyncInitialMessagesHttpResponse>>();

            CreateMap<GetChatMessagesModelResponse, GeChatMessagesHttpResponse>();
            CreateMap<BaseResponse<List<GetChatMessagesModelResponse>>, BaseResponse<List<GeChatMessagesHttpResponse>>>();


            CreateMap<NotifyLocalServerHttpRequest, NotifyLocalServerModelRequest>();
            CreateMap<NotifyLocalServerModelResponse, NotifyLocalServerHttpResponse>();
            CreateMap<BaseResponse<NotifyLocalServerModelResponse>, BaseResponse<NotifyLocalServerHttpResponse>>();


            CreateMap<SendChatMessagesHttpRequest, SendChatMessageModelRequest>();
            CreateMap<SendChatMessageModelResponse, SendChatMessagesHttpResponse>();
            CreateMap<BaseResponse<SendChatMessageModelResponse>, BaseResponse<SendChatMessagesHttpResponse>>();

        }
    }
}
