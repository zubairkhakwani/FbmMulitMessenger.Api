using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Profile;
using FBMMultiMessenger.Buisness.Request.Proxy;
using FBMMultiMessenger.Contracts.Contracts.Proxy;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Contracts.Shared;

namespace FBMMultiMessenger.Buisness.Mapping.Proxy
{
    public class ProxyProfile : Profile
    {
        public ProxyProfile()
        {
            CreateMap<GetMyProxiesHttpRequest, GetMyProxiesModelRequest>();
            CreateMap<GetMyProxiesModelResponse, GetMyProxiesHttpResponse>();
            CreateMap<PageableResponse<GetMyProxiesModelResponse>, PageableResponse<GetMyProxiesHttpResponse>>();

            CreateMap<BaseResponse<PageableResponse<GetMyProxiesModelResponse>>, BaseResponse<PageableResponse<GetMyProxiesHttpResponse>>>();


            CreateMap<UpsertProxyHttpRequest, UpsertProxyModelRequest>();
            CreateMap<UpsertProxyModelResponse, UpsertProxyHttpResponse>();
            CreateMap<BaseResponse<UpsertProxyModelResponse>, BaseResponse<UpsertProxyHttpResponse>>();
        }
    }
}
