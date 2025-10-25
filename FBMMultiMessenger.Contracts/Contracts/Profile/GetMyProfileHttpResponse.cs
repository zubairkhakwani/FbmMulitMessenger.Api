using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Profile
{
    public class GetMyProfileHttpResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }
}
