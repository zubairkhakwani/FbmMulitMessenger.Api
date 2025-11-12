using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Auth;
using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Contracts.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Mapping.Auth
{
    public class AuthProfile : Profile
    {
        public AuthProfile()
        {
            CreateMap<LoginHttpRequest, LoginModelRequest>();
            CreateMap<LoginModelResponse, LoginHttpResponse>();
            CreateMap<BaseResponse<LoginModelResponse>, BaseResponse<LoginHttpResponse>>();


            CreateMap<RegisterHttpRequest, RegisterModelRequest>();
            CreateMap<RegisterModelResponse, RegisterHttpResponse>();
            CreateMap<BaseResponse<RegisterModelResponse>, BaseResponse<RegisterHttpResponse>>();

            CreateMap<ResetPasswordHttpRequest, ResetPasswordModelRequest>();

        }
    }
}
