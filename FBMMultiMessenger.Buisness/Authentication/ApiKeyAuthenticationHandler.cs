using FBMMultiMessenger.Buisness.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace FBMMultiMessenger.Buisness.Authentication
{
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly AesEncryptionHelper _aesEncryptionHelper;

        public ApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            AesEncryptionHelper aesEncryptionHelper)
            : base(options, logger, encoder)
        {
            _aesEncryptionHelper = aesEncryptionHelper;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(ApiKeyDefaults.HeaderName, out var headerValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var rawKey = headerValues.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(rawKey))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!ApiKeyHelper.TryParseKey(_aesEncryptionHelper, rawKey, out var payload) || payload is null)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
            }

            var claims = new[]
            {
                new Claim("Id", payload.Id.ToString()),
                new Claim(ClaimTypes.Name, string.Empty),
                new Claim(ClaimTypes.Email, string.Empty),
                new Claim(ClaimTypes.Role, string.Empty),
            };

            var identity = new ClaimsIdentity(claims, ApiKeyDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, ApiKeyDefaults.AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
