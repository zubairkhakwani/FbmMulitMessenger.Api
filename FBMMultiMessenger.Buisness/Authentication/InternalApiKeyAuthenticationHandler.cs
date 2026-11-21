using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace FBMMultiMessenger.Buisness.Authentication
{
    public class InternalApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _configuration;

        public InternalApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration) : base(options, logger, encoder)
        {
            _configuration = configuration;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(InternalApiKeyDefaults.HeaderName, out var headerValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var provided = headerValues.FirstOrDefault();
            var expected = _configuration[InternalApiKeyDefaults.ConfigKey];

            if (string.IsNullOrWhiteSpace(provided) || string.IsNullOrWhiteSpace(expected))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid internal API key."));
            }

            var providedBytes = Encoding.UTF8.GetBytes(provided.Trim());
            var expectedBytes = Encoding.UTF8.GetBytes(expected.Trim());

            if (providedBytes.Length != expectedBytes.Length
                || !CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid internal API key."));
            }

            var identity = new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "internal-service") },
                InternalApiKeyDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, InternalApiKeyDefaults.AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
