namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class AutoReplyPricingTier
    {
        public int Id { get; set; }
        public int UptoMessages { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal SemiAnnualPrice { get; set; }
        public decimal AnnualPrice { get; set; }
    }
}
