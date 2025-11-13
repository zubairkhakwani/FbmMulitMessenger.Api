using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Auth;
using FBMMultiMessenger.Buisness.Service;
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
        private readonly IEmailService _emailService;

        public ResnedOtpModelRequestHandler(ApplicationDbContext dbContext, IEmailService emailService)
        {
            this._dbContext=dbContext;
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
            await _dbContext.PasswordResetTokens
                      .Where(x => x.UserId == user.Id)
                      .ExecuteUpdateAsync(p => p
                                               .SetProperty(m => m.IsUsed, true), cancellationToken);


            var otp = OtpManager.GenerateOTP();

            var newPasswordResetToken = new PasswordResetToken()
            {
                Email = user.Email,
                Otp = otp,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpManager.OtpExpiryDuration),
                UserId = user.Id,
            };

            await _dbContext.PasswordResetTokens.AddAsync(newPasswordResetToken, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (request.IsEmailVerification)
            {
                await _emailService.SendEmailVerificationEmailAsync(user.Email, otp, user.Name);
            }
            else
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, otp, user.Name);
            }

            return BaseResponse<object>.Success($"Verification code has been re-sent to your email.", new());
        }
    }
}
