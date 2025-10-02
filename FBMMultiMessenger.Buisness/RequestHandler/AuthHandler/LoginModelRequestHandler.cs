﻿using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Auth;
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

namespace FBMMultiMessenger.Buisness.RequestHandler.cs.AuthHandler
{
    internal class LoginModelRequestHandler : IRequestHandler<LoginModelRequest, BaseResponse<LoginModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly string _secretKey;

        public LoginModelRequestHandler(ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _dbContext=dbContext;
            _secretKey=configuration.GetValue<string>("ApiSettings:Key")!;
        }
        public async Task<BaseResponse<LoginModelResponse>> Handle(LoginModelRequest request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                                       .Include(s => s.Subscription)
                                       .FirstOrDefaultAsync(x => x.Email == request.Email && x.Password == request.Password, cancellationToken);

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


            var subscription = user.Subscription;

            if (subscription is null)
            {
                return BaseResponse<LoginModelResponse>.Error("Oh Snap, It looks like you don’t have a subscription yet. Please subscribe to continue.", redirectToPackages: true, response);
            }

            var today = DateTime.Now;
            var expiredAt = subscription.ExpiredAt;

            if (today >= expiredAt)
            {
                response.IsSubscriptionExpired = true;
                return BaseResponse<LoginModelResponse>.Error("Your subscription has expired. Please renew to continue.", redirectToPackages: true, response);
            }

            return BaseResponse<LoginModelResponse>.Success("Logged in successfully", response);
        }
    }
}

