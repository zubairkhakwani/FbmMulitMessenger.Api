namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class TrialConfiguration
    {
        public int Id { get; set; }

        public bool IsEnabled { get; set; }

        public int MaxAccounts { get; set; }

        public int DurationDays { get; set; }
    }
}
