using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Pricing
{
    public class GetAllPricingModelRequest : IRequest<BaseResponse<GetAllPricingModelResponse>>
    {
    }

    public class GetAllPricingModelResponse
    {
        public List<PricingTierModelResponse> PricingTiers { get; set; } = new List<PricingTierModelResponse>();
        public List<AccountDetailsModelResponse> AccountDetails { get; set; } = new List<AccountDetailsModelResponse>();
    }
    public class PricingTierModelResponse
    {
        public int Id { get; set; }
        public int UptoAccounts { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal SemiAnnualPrice { get; set; }
        public decimal AnnualPrice { get; set; }
    }
    public class AccountDetailsModelResponse
    {
        public string BankName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string AccountNo { get; set; } = string.Empty;
        public string IBAN { get; set; } = string.Empty;
    }
}
