using AutoMapper;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Contracts.Contracts.LocalServer;
using FBMMultiMessenger.Contracts.Shared;
using Microsoft.Extensions.Configuration;

namespace FBMMultiMessenger.Buisness.Mapping.LocalServer
{
    public class LocalServerProfile : Profile
    {
        public LocalServerProfile()
        {
            CreateMap<RegisterLocalServerHttpRequest, RegisterLocalServerModelRequest>();

            CreateMap<RegisterLocalServerModelResponse, RegisterLocalServerHttpResponse>();
            CreateMap<BaseResponse<RegisterLocalServerModelResponse>, BaseResponse<RegisterLocalServerHttpResponse>>();
        }
    }
}
