namespace FBMMultiMessenger.Contracts.Contracts.Pricing
{

    public class GetAllPricingHttpResponse
    {
        public List<PricingTierHttpResponse> PricingTiers { get; set; } = new List<PricingTierHttpResponse>();
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
    public class AccountDetailsHttpResponse
    {
        public string BankName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string AccountNo { get; set; } = string.Empty;
        public string IBAN { get; set; } = string.Empty;
    }
}
