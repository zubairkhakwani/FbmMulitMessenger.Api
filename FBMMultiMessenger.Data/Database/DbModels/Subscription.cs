using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class Subscription
    {
        public int Id { get; set; }
        public int MaxLimit { get; set; }
        public int LimitUsed { get; set; }
        public bool IsActive { get; set; }
        public bool CanRunOnOurServer { get; set; } //tells whether this subscription can run browsers on our server or not.
        public bool IsTrial { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime ExpiredAt { get; set; }


        [ForeignKey(nameof(User))]
        public int UserId { get; set; }


        //Navigation Properties
        public User User { get; set; }
    }
}


