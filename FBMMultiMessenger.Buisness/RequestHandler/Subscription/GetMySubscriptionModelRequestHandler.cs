using FBMMultiMessenger.Buisness.Request.Subscription;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.RequestHandler.Subscription
{
    internal class GetMySubscriptionModelRequestHandler : IRequestHandler<GetMySubscriptionModelRequest, BaseResponse<GetMySubscriptionModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;

        public GetMySubscriptionModelRequestHandler(ApplicationDbContext dbContext)
        {
            this._dbContext=dbContext;
        }
        public async Task<BaseResponse<GetMySubscriptionModelResponse>> Handle(GetMySubscriptionModelRequest request, CancellationToken cancellationToken)
        {
            var subscription = await _dbContext.Subscriptions
                                               .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

            if (subscription is null)
            {
                var failResponse = new GetMySubscriptionModelResponse();
                failResponse.HasActiveSubscription = false;
                failResponse.IsExpired = false;

                return BaseResponse<GetMySubscriptionModelResponse>.Error("Oh Snap, Looks like you don't have any subscription yet.", redirectToPackages: true, failResponse);
            }

            var today = DateTime.Now;
            var expiredAt = subscription.ExpiredAt;

            var response = new GetMySubscriptionModelResponse()
            {
                MaxLimit = subscription.MaxLimit,
                LimitUsed = subscription.LimitUsed,
                StartedAt = subscription.StartedAt,
                ExpiredAt = subscription.ExpiredAt,
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
