namespace FBMMultiMessenger.Contracts.Contracts.Pricing
{

    public class GetAllPricingHttpResponse
    {
        public PricingTierAvailabilityHttpResponse PricingTierAvailability { get; set; } = new PricingTierAvailabilityHttpResponse();
        public List<PricingTierHttpResponse> PricingTiers { get; set; } = new List<PricingTierHttpResponse>();
        public List<AutoReplyPricingTierHttpResponse> AutoReplyPricingTiers { get; set; } = new List<AutoReplyPricingTierHttpResponse>();
        public List<AccountDetailsHttpResponse> AccountDetails { get; set; } = new List<AccountDetailsHttpResponse>();
    }
    public class PricingTierHttpResponse
    {
        public int Id { get; set; }
        public int UptoAccounts { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal SemiAnnualPrice { get; set; }
        public decimal AnnualPrice { get; set; }
    }
    public class AutoReplyPricingTierHttpResponse
    {
        public int Id { get; set; }
        public int UptoMessages { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal SemiAnnualPrice { get; set; }
        public decimal AnnualPrice { get; set; }
    }
    public class AccountDetailsHttpResponse
    {
        public string BankName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string AccountNo { get; set; } = string.Empty;
        public string IBAN { get; set; } = string.Empty;
    }

    public class PricingTierAvailabilityHttpResponse
    {
        public int Id { get; set; }
        public bool IsMonthlyAvailable { get; set; }
        public bool IsSemiAnnualAvailable { get; set; }
        public bool IsAnnualAvailable { get; set; }
    }
}
