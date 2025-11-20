using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Pricing;
using FBMMultiMessenger.Contracts.Contracts.Pricing;
using FBMMultiMessenger.Contracts.Shared;

namespace FBMMultiMessenger.Buisness.Mapping.Pricing
{
    internal class PricingProfile : Profile
    {
        public PricingProfile()
        {
            CreateMap<GetAllPricingModelResponse, GetAllPricingHttpResponse>();
            CreateMap<BaseResponse<List<GetAllPricingModelResponse>>, BaseResponse<List<GetAllPricingHttpResponse>>>();
        }
    }
}
