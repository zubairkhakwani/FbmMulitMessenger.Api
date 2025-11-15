using FBMMultiMessenger.Data.Database.DbModels;
using System.Security.Cryptography;

namespace FBMMultiMessenger.Buisness.Helpers
{
    public static class OtpManager
    {
        public static readonly int PasswordExpiryDuration = 30; //minutes
        public static readonly int EmailExpiryDuration = 30; //minutes

    }
}
