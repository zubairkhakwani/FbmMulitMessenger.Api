using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using static FBMMultiMessenger.Utility.SD;

namespace FBMMultiMessenger.Request
{
    public class ApiRequest<T> where T : class
    {
        public required ApiType ApiType { get; set; } = ApiType.GET;
        public required string Url { get; set; }
        public T? Data { get; set; }
    }
}
