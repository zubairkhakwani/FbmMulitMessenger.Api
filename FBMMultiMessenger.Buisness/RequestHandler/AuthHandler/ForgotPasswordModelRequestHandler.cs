using FBMMultiMessenger.Buisness.Request.Auth;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace FBMMultiMessenger.Buisness.RequestHandler.AuthHandler
{
    internal class ForgotPasswordModelRequestHandler : IRequestHandler<ForgotPasswordModelRequest, BaseResponse<object>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;

        public ForgotPasswordModelRequestHandler(ApplicationDbContext dbContext, IEmailService emailService)
        {
            this._dbContext=dbContext;
            this._emailService=emailService;
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


            if (!request.IsResendRequest)
            {
                // If the most recent password reset token is still active and unused, 
                // do not generate a new one. Instead, notify the user that an OTP 
                // has already been sent to their email.

                var passwordResetTokens = user.PasswordResetTokens;
                var lastResetToken = passwordResetTokens
                                                .OrderByDescending(x => x.CreatedAt)
                                                .FirstOrDefault();

                var expiresAt = lastResetToken?.ExpiresAt;
                var today = DateTime.UtcNow;

                var isPreviousOtpValid = expiresAt > today && (lastResetToken is not null && !lastResetToken.IsUsed);

                if (isPreviousOtpValid)
                {
                    return BaseResponse<object>.Success("A verification code has already been sent to your email. Please check your inbox.", new());
                }
            }

            var otp = GenerateOTP();

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
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                UserId = user.Id

            };

            if (request.IsResendRequest)
            {
                await _dbContext.PasswordResetTokens
                    .Where(x => x.UserId == user.Id)
                    .ExecuteUpdateAsync(p => p
                                       .SetProperty(m => m.IsUsed, true), cancellationToken);
            }

            await _dbContext.PasswordResetTokens.AddAsync(newPasswordResetToken, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<object>.Success("A verification code has been sent successfully.", new());
        }

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
    }
}
