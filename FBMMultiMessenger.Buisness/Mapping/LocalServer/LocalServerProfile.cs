using AutoMapper;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Contracts.Contracts.LocalServer;
using FBMMultiMessenger.Contracts.Shared;

namespace FBMMultiMessenger.Buisness.Mapping.LocalServer
{
    public class LocalServerProfile : Profile
    {
        public LocalServerProfile()
        {
            CreateMap<RegisterLocalServerHttpRequest, RegisterLocalServerModelRequest>();

            CreateMap<RegisterLocalServerModelResponse, RegisterLocalServerHttpResponse>();
            CreateMap<BaseResponse<RegisterLocalServerModelResponse>, BaseResponse<RegisterLocalServerHttpResponse>>();

            CreateMap<GetLocalServerAccountsModelResponse, GetLocalServerAccountsHttpResponse>();
            CreateMap<BaseResponse<List<GetLocalServerAccountsModelResponse>>, BaseResponse<List<GetLocalServerAccountsHttpResponse>>>();

            CreateMap<LocalServerHeartBeatHttpRequet, LocalServerHeartbeatModelRequest>();
            CreateMap<LocalServerHeartbeatModelResponse, LocalServerHeartBeatHttpResponse>();
            CreateMap<BaseResponse<LocalServerHeartbeatModelResponse>, BaseResponse<LocalServerHeartBeatHttpResponse>>();
        }
    }
}
