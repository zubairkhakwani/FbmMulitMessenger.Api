using Blazored.LocalStorage;
using FBMMultiMessenger.Utility;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

namespace FBMMultiMessenger.Helpers
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService localStorageService;

        public CustomAuthenticationStateProvider(ILocalStorageService LocalStorageService)
        {
            localStorageService = LocalStorageService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var authStateTask = await GetAuthState();

            return authStateTask;
        }

        public void NotifyStateChanged()
        {
            var authStateTask = GetAuthState();

            NotifyAuthenticationStateChanged(authStateTask);
        }

        private async Task<AuthenticationState> GetAuthState()
        {
            var savedToken = await localStorageService.GetItemAsStringAsync(SD.AccessToken);

            if (string.IsNullOrWhiteSpace(savedToken))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = await ParseClaimsFromJwt(savedToken);

            if (claims.Count() == 0)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            var authStateTask = new AuthenticationState(claimsPrincipal);

            return authStateTask;
        }

        public void MarkUserAsLoggedOut()
        {
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymousUser));
            NotifyAuthenticationStateChanged(authState);
        }

        private async Task<IEnumerable<Claim>> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            try
            {
                var payload = jwt.Split('.')[1];
                var jsonBytes = ParseBase64WithoutPadding(payload);
                var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

                keyValuePairs.TryGetValue(ClaimTypes.Role, out object roles);

                if (roles != null)
                {
                    if (roles.ToString().Trim().StartsWith("["))
                    {
                        var parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString());

                        foreach (var parsedRole in parsedRoles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, parsedRole));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, roles.ToString()));
                    }

                    keyValuePairs.Remove(ClaimTypes.Role);
                }

                var tokenExpiryTimeString = keyValuePairs[ClaimTypes.Expiration].ToString();
                var parseSuccess = DateTime.TryParse(tokenExpiryTimeString, out DateTime tokenExpiryTime);

                if (parseSuccess && tokenExpiryTime < DateTime.Now)
                {
                    return claims;
                }

                claims.AddRange(keyValuePairs.Select(kvp => GetClaim(kvp.Key, kvp.Value.ToString())));
            }
            catch (Exception ex)
            {

            }

            return claims;
        }

        private Claim GetClaim(string key, string value)
        {
            return new Claim(key, value);
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}
