using Microsoft.AspNetCore.Authorization;

namespace FBMMultiMessenger.Buisness.Authentication
{
    /// <summary>
    /// Server-to-server auth for portal/admin callers. Not for customer API keys.
    /// </summary>
    public sealed class InternalApiKeyAuthorizeAttribute : AuthorizeAttribute
    {
        public InternalApiKeyAuthorizeAttribute()
        {
            AuthenticationSchemes = InternalApiKeyDefaults.AuthenticationScheme;
        }
    }
}
