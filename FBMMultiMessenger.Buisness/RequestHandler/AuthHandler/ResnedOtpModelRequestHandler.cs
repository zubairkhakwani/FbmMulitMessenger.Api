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
    internal class ResnedOtpModelRequestHandler : IRequestHandler<ResendOtpModelRequest, BaseResponse<object>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IVerificationCodeService _verificationCodeService;
        private readonly IEmailService _emailService;

        public ResnedOtpModelRequestHandler(ApplicationDbContext dbContext, IVerificationCodeService verificationCodeService, IEmailService emailService)
        {
            this._dbContext=dbContext;
            this._verificationCodeService=verificationCodeService;
            this._emailService=emailService;
        }
        public async Task<BaseResponse<object>> Handle(ResendOtpModelRequest request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                                       .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

            if (user is null)
            {
                return BaseResponse<object>.Error("Invalid request, Email does not exist");
            }

            //if the user is requesting another otp we mark previous otp as used.
            await _dbContext.VerificationTokens
                      .Where(x => x.UserId == user.Id)
                      .ExecuteUpdateAsync(p => p
                                               .SetProperty(m => m.IsUsed, true), cancellationToken);


            var otp = _verificationCodeService.GenerateOTP();

            var expiryDuration = request.IsEmailVerification
                                        ? OtpManager.EmailExpiryDuration
                                        : OtpManager.PasswordExpiryDuration;

            var newPasswordResetToken = new VerificationToken()
            {
                Email = user.Email,
                Otp = otp,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryDuration),
                UserId = user.Id,
                IsEmailVerification = request.IsEmailVerification
            };

            await _dbContext.VerificationTokens.AddAsync(newPasswordResetToken, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (request.IsEmailVerification)
            {
                await _emailService.SendEmailVerificationAsync(user.Email, otp, user.Name);
            }
            else
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, otp, user.Name);
            }

            return BaseResponse<object>.Success($"Verification code has been re-sent to your email.", new());
        }
    }
}
