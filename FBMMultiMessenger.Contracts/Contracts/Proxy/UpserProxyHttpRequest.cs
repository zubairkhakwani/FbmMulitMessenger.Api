using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Contracts.Contracts.Proxy
{
    public class UpsertProxyHttpRequest
    {
        [Required]
        public string Ip_Port { get; set; } = string.Empty;
        [Required]

        public string Name { get; set; } = string.Empty;
        [Required]

        public string Password { get; set; } = string.Empty;
    }

    public class UpsertProxyHttpResponse
    {

    }
}
