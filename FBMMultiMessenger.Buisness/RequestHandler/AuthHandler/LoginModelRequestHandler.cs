using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Models;
using FBMMultiMessenger.Buisness.Request.Auth;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FBMMultiMessenger.Buisness.RequestHandler.cs.AuthHandler
{
    internal class LoginModelRequestHandler : IRequestHandler<LoginModelRequest, BaseResponse<LoginModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly string _secretKey;

        public LoginModelRequestHandler(ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _secretKey = configuration.GetValue<string>("ApiSettings:Key")!;
        }

        public async Task<BaseResponse<LoginModelResponse>> Handle(LoginModelRequest request, CancellationToken cancellationToken)
        {
            request.Email = request.Email.ToLowerInvariant();

            // Fetch user from database
            var user = await _dbContext.Users
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
            var subscriptionCount = userSubscriptions?.Count ?? 0;

            var hasAnySubscription = userSubscriptions?.Any() ?? false;

            if (!hasAnySubscription)
            {
                return BaseResponse<LoginModelResponse>.Error(
                    "Oh Snap, It looks like you don't have a subscription yet. Ready to unlock the full experience? Subscribe now to unlock powerful features and take your experience to the next level!",
                    redirectToPackages: true,
                    response);
            }

            // Find active subscription
            var activeSubscription = userSubscriptions?
                                                .Where(x => x.StartedAt <= today && x.ExpiredAt > today)
                                                .OrderByDescending(x => x.StartedAt)
                                                .FirstOrDefault();

            if (activeSubscription is null)
            {
                var lastSubscription = userSubscriptions?
                                        .OrderByDescending(x => x.ExpiredAt)
                                        .FirstOrDefault();

                response.IsSubscriptionExpired = true;
                return BaseResponse<LoginModelResponse>.Error(
                    "Oops! Your subscription has expired. Renew today to pick up right where you left off!",
                    redirectToPackages: true,
                    response);
            }

            var daysRemaining = (activeSubscription.ExpiredAt - today).Days;

            var expiredAt = activeSubscription.ExpiredAt;

            if (today >= expiredAt)
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

