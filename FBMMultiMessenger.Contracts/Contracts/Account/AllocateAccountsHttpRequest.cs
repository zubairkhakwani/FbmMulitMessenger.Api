using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class AllocateAccountsHttpRequest
    {
        [Required]
        public int Count { get; set; }

        [Required]
        public string LocalServerId { get; set; } = null!;

    }

    public class AllocateAccountsHttpResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Cookie { get; set; }
        public string? DefaultMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
