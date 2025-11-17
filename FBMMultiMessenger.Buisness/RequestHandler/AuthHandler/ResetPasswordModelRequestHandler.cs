using FBMMultiMessenger.Buisness.Request.Auth;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AuthHandler
{
    internal class ResetPasswordModelRequestHandler : IRequestHandler<ResetPasswordModelRequest, BaseResponse<object>>
    {
        private readonly ApplicationDbContext _dbContext;

        public ResetPasswordModelRequestHandler(ApplicationDbContext dbContext)
        {
            this._dbContext=dbContext;
        }
        public async Task<BaseResponse<object>> Handle(ResetPasswordModelRequest request, CancellationToken cancellationToken)
        {
            var passwordResetToken = await _dbContext.VerificationTokens
                                                .Include(u => u.User)
                                               .FirstOrDefaultAsync(x => x.Otp == request.Otp
                                                                    &&
                                                                    !x.IsUsed, cancellationToken);

            if (passwordResetToken is null)
            {
                return BaseResponse<object>.Error("Invalid request, verification code is not valid");
            }


            if (request.NewPassword != request.ConfirmPassword)
            {
                return BaseResponse<object>.Error("Invalid request, password do not match");
            }

            var expiredAt = passwordResetToken.ExpiresAt;
            var today = DateTime.UtcNow;

            if (today > expiredAt)
            {
                return BaseResponse<object>.Error("Your verification code has expired. Please request a new one.");
            }

            //Extra safety check, as we are doing this above.
            if (passwordResetToken.IsUsed)
            {
                return BaseResponse<object>.Error("This verification code has already been used. Please request a new one.");

            }

            var user = passwordResetToken.User;

            await _dbContext.VerificationTokens
                      .Where(x => x.UserId == user.Id)
                      .ExecuteUpdateAsync(p => p
                                               .SetProperty(m => m.IsUsed, true), cancellationToken);

            user.Password = request.NewPassword;
            passwordResetToken.IsUsed = true;
            passwordResetToken.UsedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<object>.Success("Password has been changed successfully.", new());
        }
    }
}
