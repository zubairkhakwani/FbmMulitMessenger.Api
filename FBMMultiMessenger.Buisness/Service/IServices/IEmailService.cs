using FBMMultiMessenger.Buisness.Models.SignalR.App;

namespace FBMMultiMessenger.Buisness.Service.IServices
{
    public interface IEmailService
    {
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string otp, string userName);
        Task<bool> SendEmailVerificationEmailAsync(string toEmail, string otp, string userName);
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName, bool hasAvailedTrial, int trialAccounts, int trialDurationDays);

        Task<bool> SendAccountLogoutEmailAsync(string toEmail, string userName, List<AccountStatusSignalRModel> accounts);

    }
}
