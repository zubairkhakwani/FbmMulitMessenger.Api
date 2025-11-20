namespace FBMMultiMessenger.Contracts.Contracts.Pricing
{
    public class GetAllPricingHttpResponse
    {
        public int MinAccounts { get; set; }
        public int MaxAccounts { get; set; }
        public decimal PricePerAccount { get; set; }
    }
}
