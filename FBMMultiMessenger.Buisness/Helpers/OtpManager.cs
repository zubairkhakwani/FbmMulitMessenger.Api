using FBMMultiMessenger.Data.Database.DbModels;
using System.Security.Cryptography;

namespace FBMMultiMessenger.Buisness.Helpers
{
    public static class OtpManager
    {
        public static readonly int OtpExpiryDuration = 30; //minutes
        public static string GenerateOTP()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                // Available digits 0-9
                var availableDigits = Enumerable.Range(0, 10).ToList();
                var code = new int[6];

                // Select 6 unique random digits
                for (int i = 0; i < 6; i++)
                {
                    byte[] randomByte = new byte[1];
                    rng.GetBytes(randomByte);

                    // Get random index from remaining available digits
                    int index = randomByte[0] % availableDigits.Count;
                    code[i] = availableDigits[index];

                    // Remove used digit to ensure uniqueness
                    availableDigits.RemoveAt(index);
                }

                return string.Join("", code);
            }
        }

        public static bool HasValidOtp(User user, bool isEmailVerificationCode = false)
        {
            if (user?.PasswordResetTokens == null || !user.PasswordResetTokens.Any())
                return false;

            // Get the most recent token
            var lastToken = user.PasswordResetTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefault();

            if (lastToken == null)
                return false;

            // If we're checking for an email verification OTP, filter accordingly
            if (isEmailVerificationCode && !lastToken.IsEmailVerification)
                return false;

            // If we're checking for non email verification OTP, filter accordingly
            if (!isEmailVerificationCode && lastToken.IsEmailVerification)
                return false;

            var now = DateTime.UtcNow;

            // OTP is valid if it's not used and hasn't expired yet
            bool isValid = !lastToken.IsUsed && lastToken.ExpiresAt > now;

            return isValid;
        }

    }
}
