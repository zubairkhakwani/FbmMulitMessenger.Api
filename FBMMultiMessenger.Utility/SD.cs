using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Utility
{
    public class SD
    {
        public enum ApiType
        {
            GET,
            POST,
            PUT,
            DELETE
        }

        public enum ContentType
        {
            Json,
            MultipartFormData
        }

        public enum Roles
        {
            Admin = 1,
            Customer = 2,
        }


        public static string AccessToken = "JWTToken";
    }
}
