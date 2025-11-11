using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Request.Extension;
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

            CreateMap<GetChatMessagesModelResponse, GeChatMessagesHttpResponse>();
            CreateMap<BaseResponse<List<GetChatMessagesModelResponse>>, BaseResponse<List<GeChatMessagesHttpResponse>>>();


            CreateMap<NotifyExtensionHttpRequest, NotifyExtensionModelRequest>();
            CreateMap<NotifyExtensionModelResponse, NotifyExtensionHttpResponse>();
            CreateMap<BaseResponse<NotifyExtensionModelResponse>, BaseResponse<NotifyExtensionHttpResponse>>();


            CreateMap<SendChatMessagesHttpRequest, SendChatMessageModelRequest>();
            CreateMap<SendChatMessageModelResponse, SendChatMessagesHttpResponse>();
            CreateMap<BaseResponse<SendChatMessageModelResponse>, BaseResponse<SendChatMessagesHttpResponse>>();

        }
    }
}
