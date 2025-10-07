using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FBMMultiMessenger.Helpers
{
    public static class AuthenticationHelper
    {
        public static void HandleUnAuthorizedAccess(AuthenticationState context, NavigationManager navigationManager)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                navigationManager.NavigateTo("/login");

                return;
            }

            var hasActiveSubscription = context.User.FindFirst("hasActiveSubscription")?.Value != null;
            var isSubscriptionExpired = context.User.FindFirst("isSubscriptionExpired")?.Value != null;

            if (!hasActiveSubscription)
            {
                navigationManager.NavigateTo($"/packages?isExpired={isSubscriptionExpired}");
                return;
            }

        }
    }
}
