using System.Security.Cryptography;

namespace FBMMultiMessenger.Buisness.Helpers
{
    public static class ApiKeyHelper
    {
        // Prefix keeps a key recognisable in an X-API-KEY header, in logs and in support tickets.
        public static readonly string KeyPrefix = "FBM_";

        private static readonly int KeySizeInBytes = 32;

        public static string GenerateKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(KeySizeInBytes);

            var value = Convert.ToBase64String(bytes)
                               .Replace("+", string.Empty)
                               .Replace("/", string.Empty)
                               .Replace("=", string.Empty);

            return $"{KeyPrefix}{value}";
        }
    }
}
