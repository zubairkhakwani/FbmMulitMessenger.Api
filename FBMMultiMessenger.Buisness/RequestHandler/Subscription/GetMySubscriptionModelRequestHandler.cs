using FBMMultiMessenger.Buisness.Request.Subscription;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.Subscription
{
    internal class GetMySubscriptionModelRequestHandler : IRequestHandler<GetMySubscriptionModelRequest, BaseResponse<GetMySubscriptionModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public GetMySubscriptionModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<GetMySubscriptionModelResponse>> Handle(GetMySubscriptionModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check: If the user has came to this point he will be logged in hence currentuser will never be null.
            if (currentUser is null)
            {
                return BaseResponse<GetMySubscriptionModelResponse>.Error("Invalid Request, Please login again to continue.");
            }


            var activeSubscription = await _dbContext.Subscriptions
                                               .Where(x => x.StartedAt <= DateTime.UtcNow && x.ExpiredAt > DateTime.UtcNow
                                                      && x.UserId == currentUser.Id)
                                               .OrderByDescending(x => x.StartedAt)
                                               .FirstOrDefaultAsync(cancellationToken);

            if (activeSubscription is null)
            {
                var failResponse = new GetMySubscriptionModelResponse();
                failResponse.HasActiveSubscription = false;
                failResponse.IsExpired = false;

                return BaseResponse<GetMySubscriptionModelResponse>.Error("Oh Snap, Looks like you don't have any subscription yet.", redirectToPackages: true, failResponse);
            }

            var today = DateTime.Now;
            var expiredAt = activeSubscription.ExpiredAt;

            var response = new GetMySubscriptionModelResponse()
            {
                MaxLimit = activeSubscription.MaxLimit,
                LimitUsed = activeSubscription.LimitUsed,
                StartedAt = activeSubscription.StartedAt,
                ExpiredAt = activeSubscription.ExpiredAt,
            };

            if (today >= expiredAt)
            {
                response.HasActiveSubscription = false;
                response.IsExpired = true;

                return BaseResponse<GetMySubscriptionModelResponse>.Error("Your subscription has expired. Please renew to continue.", redirectToPackages: true, response);
            }

            response.HasActiveSubscription = true;
            return BaseResponse<GetMySubscriptionModelResponse>.Success("You have an active subscription", response);

        }
    }
}
