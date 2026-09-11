using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FBMMultiMessenger.Buisness.Helpers
{
    public class ApiKeyPayload
    {
        [JsonPropertyName("i")]
        public int Id { get; set; }

        [JsonPropertyName("e")]
        public long Exp { get; set; }

        [JsonPropertyName("j")]
        public string Jti { get; set; } = string.Empty;
    }

    public static class ApiKeyHelper
    {
        // Prefix keeps a key recognisable in an X-API-KEY header, in logs and in support tickets.
        public static readonly string KeyPrefix = "FBM_";

        public static readonly int KeyLifetimeYears = 10;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = null
        };

        public static string GenerateKey(AesEncryptionHelper aes, int userId, out DateTime expiresAt)
        {
            expiresAt = DateTime.UtcNow.AddYears(KeyLifetimeYears);

            var payload = new ApiKeyPayload
            {
                Id = userId,
                Exp = new DateTimeOffset(expiresAt).ToUnixTimeSeconds(),
                Jti = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant()
            };

            var plainText = JsonSerializer.Serialize(payload, JsonOptions);
            var cipherText = aes.Encrypt(plainText);
            var urlSafe = ToUrlSafeBase64(cipherText);

            return $"{KeyPrefix}{urlSafe}";
        }

        public static bool TryParseKey(AesEncryptionHelper aes, string? rawKey, out ApiKeyPayload? payload)
        {
            payload = null;

            if (string.IsNullOrWhiteSpace(rawKey) || !rawKey.StartsWith(KeyPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                var urlSafe = rawKey.Substring(KeyPrefix.Length);
                var cipherText = FromUrlSafeBase64(urlSafe);
                var plainText = aes.Decrypt(cipherText);
                var parsed = JsonSerializer.Deserialize<ApiKeyPayload>(plainText, JsonOptions);

                if (parsed is null || parsed.Id <= 0)
                {
                    return false;
                }

                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(parsed.Exp).UtcDateTime;
                if (expiresAt <= DateTime.UtcNow)
                {
                    return false;
                }

                payload = parsed;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ToUrlSafeBase64(string base64)
        {
            return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string FromUrlSafeBase64(string urlSafe)
        {
            var base64 = urlSafe.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }

            return base64;
        }
    }
}
