using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class Account
    {
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        public required string Name { get; set; }
        public required string FbAccountId { get; set; }
        public required string Cookie { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }


        //Navigation Properties
        public User User { get; set; } = null!;
        public List<Chat> Chats { get; set; } = new List<Chat>();
    }
}
