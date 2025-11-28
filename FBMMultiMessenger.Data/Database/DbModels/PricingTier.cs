namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class PricingTier
    {
        public int Id { get; set; }
        public int MinAccounts { get; set; }
        public int MaxAccounts { get; set; }
        public decimal PricePerAccount { get; set; }
    }
}
