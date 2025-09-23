using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Auth;
using FBMMultiMessenger.Contracts.Contracts.Auth;
using FBMMultiMessenger.Contracts.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Mapping.Auth
{
    public class LoginProfile : Profile
    {
        public LoginProfile()
        {
            CreateMap<LoginHttpRequest, LoginModelRequest>();

            CreateMap<LoginModelResponse, LoginHttpResponse>();

            CreateMap<BaseResponse<LoginModelResponse>, BaseResponse<LoginHttpResponse>>();
        }
    }
}
