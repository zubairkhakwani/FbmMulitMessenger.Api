namespace FBMMultiMessenger.Contracts.Contracts.Pricing
{
    public class GetAllPricingHttpResponse
    {
        public int MinAccounts { get; set; }
        public int MaxAccounts { get; set; }
        public decimal MonthlyPricePerAccount { get; set; }
        public decimal SemiAnnualPricePerAccount { get; set; }
        public decimal AnnualPricePerAccount { get; set; }
    }
}
