using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Contracts.Contracts.Chat;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Response;
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
            CreateMap<ReceiveChatHttpRequest, ReceiveChatModelRequest>();

            CreateMap<ReceiveChatModelResponse, ReceiveChatHttpResponse>();
            CreateMap<BaseResponse<ReceiveChatModelResponse>, BaseResponse<ReceiveChatHttpResponse>>();

            CreateMap<GetChatMessagesModelResponse, GeChatMessagesHttpResponse>();
            CreateMap<BaseResponse<List<GetChatMessagesModelResponse>>, BaseResponse<List<GeChatMessagesHttpResponse>>>();
            CreateMap<FileDataModelResponse, FileData>();


            CreateMap<NotifyExtensionHttpRequest, NotifyExtensionModelRequest>();
            CreateMap<NotifyExtensionModelResponse, NotifyExtensionHttpResponse>();
            CreateMap<BaseResponse<NotifyExtensionModelResponse>, BaseResponse<NotifyExtensionHttpResponse>>();


            CreateMap<SendChatMessagesHttpRequest, SendChatMessageModelRequest>();
            CreateMap<SendChatMessageModelResponse, SendChatMessagesHttpResponse>();
            CreateMap<BaseResponse<SendChatMessageModelResponse>, BaseResponse<SendChatMessagesHttpResponse>>();

        }
    }
}
