using Microsoft.AspNetCore.Authorization;

namespace FBMMultiMessenger.Buisness.Authentication
{
    public sealed class ApiKeyAuthorizeAttribute : AuthorizeAttribute
    {
        public ApiKeyAuthorizeAttribute()
        {
            AuthenticationSchemes =  ApiKeyDefaults.AuthenticationScheme;
        }
    }
}
