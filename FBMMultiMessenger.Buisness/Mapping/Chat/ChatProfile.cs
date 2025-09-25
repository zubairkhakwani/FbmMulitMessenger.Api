using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Chat;
using FBMMultiMessenger.Contracts.Contracts.Chat;
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


            CreateMap<SendChatMessageHttpRequest, SendChatMessageModelRequest>();
            CreateMap<SendChatMessageModelResponse, SendChatMessageHttpResponse>();
            CreateMap<BaseResponse<SendChatMessageModelResponse>, BaseResponse<SendChatMessageHttpResponse>>();
        }
    }
}
