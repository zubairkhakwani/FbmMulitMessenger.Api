using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Auth;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AuthHandler
{
    internal class ForgotPasswordModelRequestHandler : IRequestHandler<ForgotPasswordModelRequest, BaseResponse<object>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly IVerificationCodeService _verificationCodeService;

        public ForgotPasswordModelRequestHandler(ApplicationDbContext dbContext, IEmailService emailService, IVerificationCodeService verificationCodeService)
        {
            this._dbContext=dbContext;
            this._emailService=emailService;
            this._verificationCodeService=verificationCodeService;
        }
        public async Task<BaseResponse<object>> Handle(ForgotPasswordModelRequest request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                                       .Include(p => p.PasswordResetTokens)
                                       .FirstOrDefaultAsync(x => x.Email == request.Email.Trim(), cancellationToken);

            if (user is null)
            {
                return BaseResponse<object>.Error("Invalid request, Email does not exist.");
            }

            var isPreviousOtpValid = _verificationCodeService.HasValidOtp(user);

            if (isPreviousOtpValid)
            {
                return BaseResponse<object>.Success("A verification code has already been sent to your email. Please check your inbox.", new());
            }


            var otp = _verificationCodeService.GenerateOTP();

            var isEmailSend = await _emailService.SendPasswordResetEmailAsync(user.Email, otp, user.Name);

            if (!isEmailSend)
            {
                return BaseResponse<object>.Error("Something went wrong when sending email, please try later.");
            }

            var newPasswordResetToken = new PasswordResetToken()
            {
                Email = user.Email,
                Otp = otp,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpManager.PasswordExpiryDuration),
                UserId = user.Id
            };

            await _dbContext.PasswordResetTokens.AddAsync(newPasswordResetToken, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<object>.Success("A verification code has been sent successfully.", new());
        }
    }
}
