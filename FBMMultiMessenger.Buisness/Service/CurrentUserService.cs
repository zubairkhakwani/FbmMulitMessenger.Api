using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Service
{
    public class CurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor=httpContextAccessor;
        }
        public CurrentUser? GetCurrentUser()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated !=true)
            {
                return null;
            }

            var idClaim = user.FindFirst("Id")?.Value;
            var nameClaim = user.FindFirst(ClaimTypes.Name)?.Value;
            var emailClaim = user.FindFirst(ClaimTypes.Email)?.Value;
            var RoleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrWhiteSpace(idClaim))
            {
                return null;
            }


            return new CurrentUser()
            {
                Id = int.Parse(idClaim),
                Name = nameClaim ?? string.Empty,
                Email = emailClaim ?? string.Empty,
                Role = RoleClaim ?? string.Empty
            };
        }


        public class CurrentUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }
    }
}
