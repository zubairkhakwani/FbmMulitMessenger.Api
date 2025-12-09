using FBMMultiMessenger.Buisness.Request.Profile;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
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
        private readonly IUserAccountService _userAccountService;

        public GetMyProfileModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IUserAccountService userAccountService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
            this._userAccountService=userAccountService;
        }
        public async Task<BaseResponse<GetMyProfileModelResponse>> Handle(GetMyProfileModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check if the user has came to this point he will be logged in hence currentuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<GetMyProfileModelResponse>.Error("Invalid Request, Please login again to continue");
            }

            var user = await _dbContext.Users
                                       .Include(s => s.Subscriptions)
                                       .FirstOrDefaultAsync(x => x.Id == currentUser.Id, cancellationToken);

            if (user is null)
            {
                return BaseResponse<GetMyProfileModelResponse>.Error("Invalid Request, User does not exist");
            }

            var activeSubscription = _userAccountService.GetActiveSubscription(user.Subscriptions);

            if (activeSubscription is null)
            {
                activeSubscription = _userAccountService.GetLastActiveSubscription(user.Subscriptions);
            }

            var formattedStartDate = activeSubscription?.StartedAt.ToLocalTime().ToString("MMM d, yyyy") ?? string.Empty;
            var formattedExpiredAt = activeSubscription?.ExpiredAt.ToLocalTime().ToString("MMM d, yyyy") ?? string.Empty;

            DateTime? expirationDate = activeSubscription?.ExpiredAt;
            DateTime currentDate = DateTime.UtcNow;
            TimeSpan? difference = expirationDate - currentDate;

            int days = difference?.Days ?? 0;
            int hours = difference?.Hours ?? 0;
            string dayText = days == 1 ? "day" : "days";
            string hourText = hours == 1 ? "hour" : "hours";
            string message = string.Empty;

            if (days < 0 || (days == 0 && hours < 0))
            {
                message = "Subscription expired";
            }
            else if (days > 0)
            {
                message = hours > 0
                    ? $"{days} {dayText} and {hours} {hourText} remaining"
                    : $"{days} {dayText} remaining";
            }
            else
            {
                message = hours > 0
                    ? $"{hours} {hourText} remaining"
                    : "Less than 1 hour remaining";
            }


            var response = new GetMyProfileModelResponse()
            {
                Name = user.Name,
                ContactNumber = user.ContactNumber,
                Email = user.Email,
                JoinedAt = user.CreatedAt,
                ExpiredAt = formattedExpiredAt,
                StartedAt = formattedStartDate,
                RemainingTimeText = message,
                RemainingDaysCount = days
            };

            return BaseResponse<GetMyProfileModelResponse>.Success("Operation performed successfully", response);
        }
    }
}
