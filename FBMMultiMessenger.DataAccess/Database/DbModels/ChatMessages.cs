using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class ChatMessages
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Chat))]
        public int ChatId { get; set; }

        public required string Message { get; set; }

        public bool IsReceived { get; set; } // if true then the message was recevied other wise message was sent.

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        //Navigation Properties
        public Chat Chat { get; set; } = null!;
    }
}
