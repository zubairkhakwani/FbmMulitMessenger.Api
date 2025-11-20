using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class Subscription
    {
        public int Id { get; set; }
        public int MaxLimit { get; set; }
        public int LimitUsed { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime ExpiredAt { get; set; }


        [ForeignKey(nameof(User))]
        public int UserId { get; set; }


        //Navigation Properties
        public User User { get; set; }
    }
}


