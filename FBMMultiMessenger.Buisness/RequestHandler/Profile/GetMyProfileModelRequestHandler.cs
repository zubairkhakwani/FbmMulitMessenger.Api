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
    internal class GetMyProfileModelRequestHandler : IRequestHandler<GetMyProfileModelRequest, BaseResponse<GetMyProfileModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public GetMyProfileModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<GetMyProfileModelResponse>> Handle(GetMyProfileModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check if the user has came to this point he will be logged in hence currentuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<GetMyProfileModelResponse>.Error("Invalid Request, Please login again to continue");
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == currentUser.Id, cancellationToken);

            if (user is null)
            {
                return BaseResponse<GetMyProfileModelResponse>.Error("Invalid Request, User does not exist");
            }

            var response = new GetMyProfileModelResponse()
            {
                Name = user.Name,
                ContactNumber = user.ContactNumber,
                Email = user.Email,
                JoinedAt = user.CreatedAt
            };

            return BaseResponse<GetMyProfileModelResponse>.Success("Operation performed successfully", response);
        }
    }
}
