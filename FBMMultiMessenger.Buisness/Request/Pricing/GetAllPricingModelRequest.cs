using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using OneSignal.RestAPIv3.Client.Resources;

namespace FBMMultiMessenger.Buisness.Request.Pricing
{
    public class GetAllPricingModelRequest : IRequest<BaseResponse<List<GetAllPricingModelResponse>>>
    {
    }

    public class GetAllPricingModelResponse
    {
        public int MinAccounts { get; set; }
        public int MaxAccounts { get; set; }
        public decimal PricePerAccount { get; set; }
    }
}
