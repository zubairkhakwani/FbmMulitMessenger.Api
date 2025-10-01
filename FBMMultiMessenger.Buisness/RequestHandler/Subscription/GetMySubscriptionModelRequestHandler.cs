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
                return BaseResponse<GetMySubscriptionModelResponse>.Error("Oops, Looks like you dont have any subscription yet.", isSubscriptionActive: false);
            }

            var startedAt = subscription.StartedAt;
            var expiredAt = subscription.ExpiredAt;

            if (startedAt >= expiredAt  && !subscription.IsExpired)
            {
                subscription.IsExpired = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var response = new GetMySubscriptionModelResponse()
            {
                MaxLimit = subscription.MaxLimit,
                LimitUsed = subscription.LimitUsed,
                StartedAt = subscription.StartedAt,
                ExpiredAt = subscription.ExpiredAt,
                IsExpired  = subscription.IsExpired
            };

            var responseMessage = subscription.IsExpired ? "Your subscription has expired. Please renew to continue." : "Subscription Found";

            return BaseResponse<GetMySubscriptionModelResponse>.Success(responseMessage, response, isSubscriptionExpired: subscription.IsExpired);

        }
    }
}
