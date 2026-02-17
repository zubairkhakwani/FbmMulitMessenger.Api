namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class PricingTierAvailability
    {
        public int Id { get; set; }
        public bool IsMonthlyAvailable { get; set; } = true;
        public bool IsSemiAnnualAvailable { get; set; } = true;
        public bool IsAnnualAvailable { get; set; } = true;
    }
}
