using FBMMultiMessenger.Buisness.Request.Profile;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.RequestHandler.Profile
{
    public class EditProfileModelRequestHandler : IRequestHandler<EditProfileModelRequest, BaseResponse<object>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public EditProfileModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<object>> Handle(EditProfileModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            if (currentUser is null)
            {
                return BaseResponse<object>.Error("Invalid Request, Please login again to continue.");
            }

            var currentUserId = currentUser.Id;

            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken);

            if (user is null)
            {
                return BaseResponse<object>.Error("Invalid Request, User does not exist.");
            }

            var requestedName = request.Name.Trim();
            var requestedEmail = request.Email.Trim();
            var requestedContactNumber = request.PhoneNumber.Trim();

            if (user.Name.Trim() == requestedName && user.Email.Trim() == requestedEmail && user.ContactNumber == requestedContactNumber)
            {
                return BaseResponse<object>.Success("No changes deteced", new());
            }


            user.Name = requestedName;
            user.Email = requestedEmail;
            user.ContactNumber = requestedContactNumber;

            await _dbContext.SaveChangesAsync();

            return BaseResponse<object>.Success("Your profile has been updated successfully", new());
        }
    }
}
