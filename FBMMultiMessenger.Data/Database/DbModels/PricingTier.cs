namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class PricingTier
    {
        public int Id { get; set; }
        public int UptoAccounts { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal SemiAnnualPrice { get; set; }
        public decimal AnnualPrice { get; set; }
    }
}
