using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Models;
using FBMMultiMessenger.Buisness.Request.Auth;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FBMMultiMessenger.Buisness.RequestHandler.cs.AuthHandler
{
    internal class LoginModelRequestHandler(ApplicationDbContext dbContext, IUserAccountService _userAccountService, IConfiguration configuration) : IRequestHandler<LoginModelRequest, BaseResponse<LoginModelResponse>>
    {
        private readonly string _secretKey = configuration.GetValue<string>("ApiSettings:Key")!;

        public async Task<BaseResponse<LoginModelResponse>> Handle(LoginModelRequest request, CancellationToken cancellationToken)
        {
            request.Email = request.Email.ToLowerInvariant();

            // Fetch user from database
            var user = await dbContext.Users
                                       .Include(r => r.Role)
                                       .Include(s => s.Subscriptions)
                                       .FirstOrDefaultAsync(x => x.Email == request.Email.Trim().ToLower() && x.Password == request.Password, cancellationToken);

            // Check if user exists
            if (user is null)
            {
                return BaseResponse<LoginModelResponse>.Error("Invalid email or password.");
            }

            // Generate JWT Token
            var generateTokenModel = new GenerateJWTModel()
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role.Name.ToString(),
                Key = _secretKey
            };

            var token = JWTHelper.GenerateAccessToken(generateTokenModel);

            var response = new LoginModelResponse()
            {
                UserId = user.Id,
                Token = token.accessToken
            };

            var today = DateTime.UtcNow;

            // Check subscriptions
            var userSubscriptions = user.Subscriptions;
            var activeSubscription = _userAccountService.GetActiveSubscription(userSubscriptions);

            if (activeSubscription is null)
            {
                return BaseResponse<LoginModelResponse>.Error(
                    "Oh Snap, It looks like you don't have a subscription yet. Ready to unlock the full experience? Subscribe now to unlock powerful features and take your experience to the next level!",
                    redirectToPackages: true,
                    response);
            }

            var isSubscriptionExpired = _userAccountService.IsSubscriptionExpired(activeSubscription);

            if (isSubscriptionExpired)
            {
                response.IsSubscriptionExpired = true;
                return BaseResponse<LoginModelResponse>.Error(
                    "Oops! Your subscription has expired. Renew today to pick up right where you left off!",
                    redirectToPackages: true,
                    response);
            }

            return BaseResponse<LoginModelResponse>.Success("Logged in successfully", response);
        }
    }
}

