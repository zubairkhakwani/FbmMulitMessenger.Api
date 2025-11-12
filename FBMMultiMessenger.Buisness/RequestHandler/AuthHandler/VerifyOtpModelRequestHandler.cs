using FBMMultiMessenger.Buisness.Request.Auth;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AuthHandler
{
    internal class VerifyOtpModelRequestHandler : IRequestHandler<VerifyOtpModelRequest, BaseResponse<object>>
    {
        private readonly ApplicationDbContext _dbContext;

        public VerifyOtpModelRequestHandler(ApplicationDbContext dbContext)
        {
            this._dbContext=dbContext;
        }
        public async Task<BaseResponse<object>> Handle(VerifyOtpModelRequest request, CancellationToken cancellationToken)
        {
            var passwordResetToken = await _dbContext.PasswordResetTokens
                                               .FirstOrDefaultAsync(x => x.Otp == request.Otp, cancellationToken);


            if (passwordResetToken is null)
            {
                return BaseResponse<object>.Error("Invalid request, verification code is not valid");
            }

            var expiredAt = passwordResetToken.ExpiresAt;
            var today = DateTime.UtcNow;

            if (today > expiredAt)
            {
                return BaseResponse<object>.Error("Your verification code has expired. Please request a new one.");
            }

            if (passwordResetToken.IsUsed)
            {
                return BaseResponse<object>.Error("This verification code has already been used. Please request a new one.");
            }

            return BaseResponse<object>.Success("Verification successful. You can now reset your password.", new());
        }
    }
}
