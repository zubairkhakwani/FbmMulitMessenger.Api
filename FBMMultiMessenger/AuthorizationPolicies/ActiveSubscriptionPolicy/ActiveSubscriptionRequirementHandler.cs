using FBMMultiMessenger.Contracts.Contracts.Subscription;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace FBMMultiMessenger.AuthorizationPolicies.ActiveSubscriptionPolicy
{
    internal class ActiveSubscriptionRequirementHandler : AuthorizationHandler<ActiveSubscriptionRequirement>
    {
        public ActiveSubscriptionRequirementHandler(ISubscriptionSerivce subscriptionSerivce)
        {
            SubscriptionSerivce = subscriptionSerivce;
        }

        public ISubscriptionSerivce SubscriptionSerivce { get; }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ActiveSubscriptionRequirement requirement)
        {
            if (!context.User.Identity.IsAuthenticated)
            {
                return;
            }

            var response = await SubscriptionSerivce.GetMySubscription<BaseResponse<GetMySubscriptionHttpResponse>>();
            var isRedirectRequest = response.RedirectToPackages;
            bool isSubscriptionExpired = response.Data?.IsExpired ?? false;
            bool hasActiveSubscription = response.Data?.HasActiveSubscription ?? false;

            var identity = context.User.Identity as ClaimsIdentity;

            if (hasActiveSubscription)
            {
                identity?.AddClaim(new Claim("hasActiveSubscription", $"{hasActiveSubscription}"));
            }

            if (isSubscriptionExpired)
            {
                identity?.AddClaim(new Claim("isSubscriptionExpired", $"{isSubscriptionExpired}"));
            }

            if (hasActiveSubscription && !isSubscriptionExpired)
            {
                await Task.Delay(5000);
                context.Succeed(requirement);
            }
        }
    }
}
