using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class Chat
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Account))]
        public int AccountId { get; set; }

        public string? FbUserId { get; set; }
        public string? FbAccountId { get; set; }
        public string? FBChatId { get; set; }
        public bool IsRead { get; set; }
        public DateTime StartedAt { get; set; }


        //Navigation Properties
        public Account Account { get; set; } = null!;
    }
}
