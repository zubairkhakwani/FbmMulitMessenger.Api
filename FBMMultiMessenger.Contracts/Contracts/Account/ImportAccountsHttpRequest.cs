using System.ComponentModel.DataAnnotations;

namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class ImportAccountsHttpRequest
    {
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public string Cookie { get; set; } = null!;
        public string? ProxyId { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
