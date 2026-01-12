using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Account
{
    public class UpsertAccountHttpRequest
    {
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public string Cookie { get; set; } = null!;
        public string? ProxyId { get; set; } = null!;
    }

    public class UpsertAccountHttpResponse
    {
        public bool IsSubscriptionExpired { get; set; }
        public bool IsEmailVerified { get; set; } = true;
        public string EmailSendTo { get; set; } = string.Empty;

        public int TotalProcessed { get; set; }
        public int SuccessfullyValidated { get; set; }
        public List<SkippedAccountHttpResponse> SkippedAccounts { get; set; } = new();
    }

    public class SkippedAccountHttpResponse
    {
        public string Name { get; set; } = string.Empty;
        public string? ProxyId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
