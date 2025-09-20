using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.RequestHandler.cs
{
    internal class LoginModelRequestHandler : IRequestHandler<LoginModelRequest, BaseResponse<LoginModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly string _secretKey;

        public LoginModelRequestHandler(ApplicationDbContext dbContext, IConfiguration configuration)
        {
            this._dbContext=dbContext;
            this._secretKey=configuration.GetValue<string>("ApiSettings:Key")!;
        }
        public async Task<BaseResponse<LoginModelResponse>> Handle(LoginModelRequest request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                                       .FirstOrDefaultAsync(x => x.Email == request.Email && x.Password == request.Password);

            if (user is null)
            {
                return BaseResponse<LoginModelResponse>.Error("Invalid email or password.");
            }

            var generateTokenModel = new GenerateJWTModel()
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Key = _secretKey
            };

            var token = JWTHelper.GenerateAccessToken(generateTokenModel);

            var response = new LoginModelResponse()
            {
                Token = token.accessToken
            };

            return BaseResponse<LoginModelResponse>.Success("Logged in successfully", response);
        }
    }
}

