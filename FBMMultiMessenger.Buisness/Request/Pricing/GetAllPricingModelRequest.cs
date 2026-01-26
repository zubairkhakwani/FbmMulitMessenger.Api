using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using OneSignal.RestAPIv3.Client.Resources;
using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Buisness.Request.Pricing
{
    public class GetAllPricingModelRequest : IRequest<BaseResponse<List<GetAllPricingModelResponse>>>
    {
    }

    public class GetAllPricingModelResponse
    {
        public int MinAccounts { get; set; }
        public int MaxAccounts { get; set; }
        public decimal MonthlyPricePerAccount { get; set; }
        public decimal SemiAnnualPricePerAccount { get; set; }
        public decimal AnnualPricePerAccount { get; set; }
    }
}
