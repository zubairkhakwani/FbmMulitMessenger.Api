namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class TrialConfiguration
    {
        public int Id { get; set; }

        public bool IsEnabled { get; set; }

        public int MaxAccounts { get; set; }

        public int DurationDays { get; set; }

        /// <summary>AI auto reply message quota granted on trial subscriptions. 0 = AI off for trial.</summary>
        public int AutoReplyMaxLimit { get; set; }
    }
}
