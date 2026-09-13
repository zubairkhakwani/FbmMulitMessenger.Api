using AutoMapper;
using FBMMultiMessenger.Buisness.Request.ApiKey;
using FBMMultiMessenger.Contracts.Contracts.ApiKey;
using FBMMultiMessenger.Contracts.Shared;

namespace FBMMultiMessenger.Buisness.Mapping.ApiKey
{
    public class ApiKeyProfile : Profile
    {
        public ApiKeyProfile()
        {
            CreateMap<GetMyApiKeyHttpRequest, GetMyApiKeyModelRequest>();
            CreateMap<GetMyApiKeyModelResponse, GetMyApiKeyHttpResponse>();
            CreateMap<BaseResponse<GetMyApiKeyModelResponse>, BaseResponse<GetMyApiKeyHttpResponse>>();

            CreateMap<UpsertApiKeyHttpRequest, UpsertApiKeyModelRequest>();
            CreateMap<UpsertApiKeyModelResponse, UpsertApiKeyHttpResponse>();
            CreateMap<BaseResponse<UpsertApiKeyModelResponse>, BaseResponse<UpsertApiKeyHttpResponse>>();
        }
    }
}
