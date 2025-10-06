using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class User
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string ContactNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }


        //Navigation Property
        public Subscription Subscription { get; set; }
        public List<Account> Accounts { get; set; } = new List<Account>();
    }
}
