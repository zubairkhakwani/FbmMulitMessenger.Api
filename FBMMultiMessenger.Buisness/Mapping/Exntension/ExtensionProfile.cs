using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Shared;

namespace FBMMultiMessenger.Buisness.Mapping.Exntension
{
    public class ExtensionProfile : Profile
    {
        public ExtensionProfile()
        {
            CreateMap<GetEncExtensionContentModelResponse, GetEncExntesionContentHttpResponse>();
            CreateMap<BaseResponse<GetEncExtensionContentModelResponse>, BaseResponse<GetEncExntesionContentHttpResponse>>();

        }
    }
}
