using FBMMultiMessenger.Buisness.Request.Profile;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.Profile
{
    internal class ChangePasswordModelRequestHandler : IRequestHandler<ChangePasswordModelRequest, BaseResponse<object>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public ChangePasswordModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<object>> Handle(ChangePasswordModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check, if the user has came to this point he will be logged in hence currentUser cant be null.
            if (currentUser is null)
            {
                return BaseResponse<object>.Error("Invalid Request, Please login again to continue.");
            }

            // Normalize input by trimming spaces and converting to lowercase 
            // to ensure case-insensitive and whitespace-tolerant password comparison.

            var newPassword = request.NewPassword.ToLower().Trim();
            var confirmPassword = request.ConfirmPassword.ToLower().Trim();
            var currentPassword = request.CurrentPassword.ToLower().Trim();

            if (currentPassword == newPassword)
            {
                return BaseResponse<object>.Error("Invalid Request, New password cannot be the same as your current password.");
            }

            if (newPassword != confirmPassword)
            {
                return BaseResponse<object>.Error("Invalid Request, Password do not match");
            }

            var user = await _dbContext.Users
                                       .FirstOrDefaultAsync(x => x.Id == currentUser.Id, cancellationToken);

            if (user is null)
            {
                return BaseResponse<object>.Error("Invalid Request, User does not exist");
            }

            if (user.Password != request.CurrentPassword)
            {
                return BaseResponse<object>.Error("The current password you entered is incorrect.");
            }

            user.Password = request.NewPassword;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<object>.Success("Your password has been updated successfully.", new());
        }

    }
}
