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

            var today = DateTime.UtcNow;

            var userSubscriptions = await _dbContext.Subscriptions
                                               .AsNoTracking()
                                               .Where(u => u.UserId == currentUser.Id).ToListAsync(cancellationToken);

            if (!userSubscriptions.Any())
            {
                var noSubscriptionResponse = new GetMySubscriptionModelResponse();
                noSubscriptionResponse.HasActiveSubscription = false;
                noSubscriptionResponse.IsExpired = false;

                return BaseResponse<GetMySubscriptionModelResponse>.Error("Oh Snap, Looks like you don't have any subscription yet.", redirectToPackages: true, result: noSubscriptionResponse);
            }

            var activeSubscription = userSubscriptions?
                                                .Where(x => x.StartedAt <= today && x.ExpiredAt > today)
                                                .OrderByDescending(x => x.StartedAt)
                                                .FirstOrDefault();

            var expiredAt = activeSubscription?.ExpiredAt;

            var response = new GetMySubscriptionModelResponse()
            {
                MaxLimit = activeSubscription?.MaxLimit ?? 0,
                LimitUsed  = activeSubscription?.LimitUsed ?? 0,
                StartedAt = activeSubscription?.StartedAt ?? new DateTime(),
                ExpiredAt = activeSubscription?.ExpiredAt ?? new DateTime(),
            };

            if (activeSubscription is null || today >= expiredAt)
            {
                response.HasActiveSubscription = false;
                response.IsExpired = true;
                return BaseResponse<GetMySubscriptionModelResponse>.Error("Oops! Your subscription has expired. Renew today to pick up right where you left off!", redirectToPackages: true, result: response);
            }

            response.IsExpired = false;
            response.HasActiveSubscription = true;
            return BaseResponse<GetMySubscriptionModelResponse>.Success("You have an active subscription", response);
        }
    }
}
