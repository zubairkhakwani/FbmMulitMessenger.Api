namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class PricingTier
    {
        public int Id { get; set; }
        public int MinAccounts { get; set; }
        public int MaxAccounts { get; set; }
        public decimal MonthlyPricePerAccount { get; set; }
        public decimal AnnualPricePerAccount { get; set; }
        public decimal SemiAnnualPricePerAccount { get; set; }
    }
}
