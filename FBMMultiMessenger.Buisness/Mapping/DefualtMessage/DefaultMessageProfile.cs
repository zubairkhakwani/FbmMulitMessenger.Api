using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Request.DefaultMessage;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Contracts.DefaultMessage;
using FBMMultiMessenger.Contracts.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Mapping.DefualtMessage
{
    internal class DefaultMessageProfile : Profile
    {
        public DefaultMessageProfile()
        {
            CreateMap<GetMyDefaultMessagesModelResponse, GetMyDefaultMessagesHttpResponse>();
            CreateMap<DefaultMessagesModelResponse, DefaultMessagesHttpResponse>();
            CreateMap<GetMyAccountsModelResponse, GetMyAccountsHttpResponse>();
            CreateMap<BaseResponse<GetMyDefaultMessagesModelResponse>, BaseResponse<GetMyDefaultMessagesHttpResponse>>();



            CreateMap<UpsertDefaultMessageHttpRequest, UpsertDefaultMessageModelRequest>();
            CreateMap<UpsertDefaultMessageModelResponse, UpsertDefaultMessageHttpResponse>();
            CreateMap<BaseResponse<UpsertDefaultMessageModelResponse>, BaseResponse<UpsertDefaultMessageHttpResponse>>();
        }
    }
}
