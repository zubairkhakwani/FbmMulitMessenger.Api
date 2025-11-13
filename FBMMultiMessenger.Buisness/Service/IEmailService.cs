namespace FBMMultiMessenger.Buisness.Service
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string otp, string userName);
        Task<bool> SendEmailVerificationEmailAsync(string toEmail, string otp, string userName);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);

    }
}
