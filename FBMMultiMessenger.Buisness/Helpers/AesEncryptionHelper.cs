using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace FBMMultiMessenger.Buisness.Helpers
{
    public class AesEncryptionHelper
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AesEncryptionHelper(IConfiguration configuration)
        {
            _key = Convert.FromBase64String(configuration["Encryption:AES:Key"]!);
            _iv = Convert.FromBase64String(configuration["Encryption:AES:IV"]!);
        }

        public string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            var cipherBytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipherBytes);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
    }
}
