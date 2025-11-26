using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Models;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;

namespace FBMMultiMessenger.Buisness.Service
{
    public class UserAccountService : IUserAccountService
    {
        private readonly IVerificationCodeService _verificationCodeService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;

        public UserAccountService(ApplicationDbContext dbContext, IEmailService emailService, IVerificationCodeService verificationCodeService)
        {
            this._verificationCodeService=verificationCodeService;
            this._dbContext=dbContext;
            this._emailService=emailService;
        }

        public Subscription? GetActiveSubscription(List<Subscription> subscriptions)
        {
            var today = DateTime.UtcNow;
            var activeSubscription = subscriptions
                                         .Where(x =>
                                            x.StartedAt <= today
                                            &&
                                            x.ExpiredAt > today)
                                         .OrderByDescending(x => x.StartedAt)
                                         .FirstOrDefault();

            return activeSubscription;
        }

        public bool HasLimitExceeded(Subscription subscription)
        {
            var maxLimit = subscription.MaxLimit;
            var limitUsed = subscription.LimitUsed;

            return limitUsed >= maxLimit;
        }

        public bool IsSubscriptionExpired(Subscription subscription)
        {
            var today = DateTime.UtcNow;
            var expiryDate = subscription?.ExpiredAt;

            return today >= expiryDate;
        }

        public async Task<BaseResponse<EmailVerificationResponse>> ProcessEmailVerificationAsync(User user, CancellationToken cancellationToken)
        {

            var isPreviousOtpValid = _verificationCodeService.HasValidOtp(user, isEmailVerificationCode: true);

            if (isPreviousOtpValid)
            {
                return BaseResponse<EmailVerificationResponse>.Success("A verification code has already been sent to your email. Please check your inbox.", result: new EmailVerificationResponse() { IsEmailVerified = false, EmailSendTo = user.Email });
            }

            var otp = _verificationCodeService.GenerateOTP();

            var newPasswordResetToken = new VerificationToken()
            {
                Email = user.Email,
                Otp = otp,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpManager.EmailExpiryDuration),
                UserId = user.Id,
                IsEmailVerification = true
            };

            await _dbContext.VerificationTokens.AddAsync(newPasswordResetToken, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _emailService.SendEmailVerificationEmailAsync(user.Email, otp, user.Name);

            return BaseResponse<EmailVerificationResponse>.Error("Please verify your email to continue.", result: new EmailVerificationResponse() { IsEmailVerified = false, EmailSendTo = user.Email });
        }
    }
}
