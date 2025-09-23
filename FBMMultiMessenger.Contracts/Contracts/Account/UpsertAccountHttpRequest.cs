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
    }

    public class UpsertAccountHttpResponse
    {
        public int UserId { get; set; }
    }
}
