using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Auth
{
    public class LoginHttpRequest
    {
        [Required]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }

    public class LoginHttpResponse
    {
        public string? Token { get; set; }
        public bool IsSubscriptionExpired { get; set; }
    }
}
