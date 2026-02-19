using FBMMultiMessenger.Contracts.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace FBMMultiMessenger.Data.Database.DbModels
{
    public class Account
    {
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        [ForeignKey(nameof(DefaultMessage))]
        public int? DefaultMessageId { get; set; }

        [ForeignKey(nameof(LocalServer))]
        public int? LocalServerId { get; set; }

        [ForeignKey(nameof(Proxy))]
        public int? ProxyId { get; set; }

        public required string Name { get; set; }
        public required string FbAccountId { get; set; }
        public required string Cookie { get; set; }
        public AccountConnectionStatus ConnectionStatus { get; set; }
        public AccountAuthStatus AuthStatus { get; set; }
        public AccountReason Reason { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }


        //Navigation Properties
        public User User { get; set; } = null!;
        public DefaultMessage? DefaultMessage { get; set; }
        public LocalServer? LocalServer { get; set; }
        public Proxy? Proxy { get; set; }
        public List<Chat> Chats { get; set; } = new List<Chat>();
    }
}
