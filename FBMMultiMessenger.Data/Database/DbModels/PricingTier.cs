namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class PricingTier
    {
        public int Id { get; set; }
        public int MinAccounts { get; set; }
        public int MaxAccounts { get; set; }
        public decimal PricePerAccount { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
