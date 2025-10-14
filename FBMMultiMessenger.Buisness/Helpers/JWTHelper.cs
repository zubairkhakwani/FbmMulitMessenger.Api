using FBMMultiMessenger.Data.Database.DbModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.Helpers
{
    public static class JWTHelper
    {
        public static (string accessToken, string accessTokenId) GenerateAccessToken(GenerateJWTModel model)
        {
            var tokenHandeler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(model.Key);
            var accessTokenId = "JTI"+Guid.NewGuid().ToString();

            var expiryDate = DateTime.Now.AddDays(30);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                  new Claim("Id",model.UserId.ToString()!),
                  new Claim(ClaimTypes.Name,model.Name!),
                  new Claim(ClaimTypes.Email,model.Email!),
                  new Claim(JwtRegisteredClaimNames.Jti, accessTokenId),
                  new Claim(ClaimTypes.Expiration, expiryDate.ToString()),
                }),
                Expires = expiryDate,
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandeler.CreateToken(tokenDescriptor);

            return (tokenHandeler.WriteToken(token), accessTokenId);
        }

    }
}
