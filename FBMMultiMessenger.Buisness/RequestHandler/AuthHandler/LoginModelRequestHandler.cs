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

        public LoginModelRequestHandler(
            ApplicationDbContext dbContext,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _secretKey = configuration.GetValue<string>("ApiSettings:Key")!;
        }

        public async Task<BaseResponse<LoginModelResponse>> Handle(LoginModelRequest request, CancellationToken cancellationToken)
        {
            var logMessages = new List<string>();

            logMessages.Add("========== LOGIN ATTEMPT STARTED ==========");
            logMessages.Add($"Login attempt for Email: {request.Email}");
            logMessages.Add($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            request.Email = request.Email.ToLowerInvariant();

            // Fetch user from database
            var user = await _dbContext.Users
                                       .Include(r => r.Role)
                                       .Include(s => s.Subscriptions)
                                       .FirstOrDefaultAsync(x => x.Email == request.Email && x.Password == request.Password, cancellationToken);

            // Check if user exists
            if (user is null)
            {
                logMessages.Add("LOGIN FAILED: Invalid credentials");
                WriteLog(logMessages, "login-failed");
                return BaseResponse<LoginModelResponse>.Error("Invalid email or password.");
            }

            logMessages.Add("✓ User found in database");
            logMessages.Add($"User ID: {user.Id}");
            logMessages.Add($"User Name: {user.Name}");
            logMessages.Add($"User Email: {user.Email}");

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
            logMessages.Add("✓ JWT token generated");

            var response = new LoginModelResponse()
            {
                UserId = user.Id,
                Token = token.accessToken
            };

            var today = DateTime.UtcNow;
            logMessages.Add($"Current Date: {today:yyyy-MM-dd HH:mm:ss}");

            // Check subscriptions
            var userSubscriptions = user.Subscriptions;
            var subscriptionCount = userSubscriptions?.Count ?? 0;

            logMessages.Add($"Total Subscriptions: {subscriptionCount}");

            // Log all subscriptions
            if (subscriptionCount > 0)
            {
                logMessages.Add("--- All Subscriptions ---");
                foreach (var sub in userSubscriptions!)
                {
                    var status = sub.ExpiredAt > today ? "Active/Future" : "Expired";
                    logMessages.Add($"  Sub ID: {sub.Id} | Started: {sub.StartedAt:yyyy-MM-dd} | Expires: {sub.ExpiredAt:yyyy-MM-dd} | Status: {status}");
                }
            }

            var hasAnySubscription = userSubscriptions?.Any() ?? false;

            if (!hasAnySubscription)
            {
                logMessages.Add("LOGIN FAILED: No subscriptions found");
                WriteLog(logMessages, "login-failed-no-subscription");
                return BaseResponse<LoginModelResponse>.Error(
                    "Oh Snap, It looks like you don't have a subscription yet. Ready to unlock the full experience? Subscribe now to unlock powerful features and take your experience to the next level!",
                    redirectToPackages: true,
                    response);
            }

            logMessages.Add("✓ User has subscriptions");

            // Find active subscription
            var activeSubscription = userSubscriptions?
                                                .Where(x => x.StartedAt <= today && x.ExpiredAt > today)
                                                .OrderByDescending(x => x.StartedAt)
                                                .FirstOrDefault();

            logMessages.Add("--- Checking Active Subscription ---");
            logMessages.Add($"Condition: StartedAt <= {today:yyyy-MM-dd} AND ExpiredAt > {today:yyyy-MM-dd}");

            if (activeSubscription is null)
            {
                logMessages.Add("LOGIN FAILED: No active subscription");

                var lastSubscription = userSubscriptions?
                                        .OrderByDescending(x => x.ExpiredAt)
                                        .FirstOrDefault();

                if (lastSubscription != null)
                {
                    logMessages.Add($"Last subscription expired on: {lastSubscription.ExpiredAt:yyyy-MM-dd}");
                }

                response.IsSubscriptionExpired = true;
                WriteLog(logMessages, "login-failed-expired");
                return BaseResponse<LoginModelResponse>.Error(
                    "Oops! Your subscription has expired. Renew today to pick up right where you left off!",
                    redirectToPackages: true,
                    response);
            }

            // Active subscription found
            logMessages.Add("✓ Active subscription found!");
            logMessages.Add($"Active Sub ID: {activeSubscription.Id}");
            logMessages.Add($"Started: {activeSubscription.StartedAt:yyyy-MM-dd HH:mm:ss}");
            logMessages.Add($"Expires: {activeSubscription.ExpiredAt:yyyy-MM-dd HH:mm:ss}");

            var daysRemaining = (activeSubscription.ExpiredAt - today).Days;
            logMessages.Add($"Days Remaining: {daysRemaining}");

            var expiredAt = activeSubscription.ExpiredAt;

            if (today >= expiredAt)
            {
                logMessages.Add("LOGIN FAILED: Subscription expired (edge case)");
                response.IsSubscriptionExpired = true;
                WriteLog(logMessages, "login-failed-edge-case");
                return BaseResponse<LoginModelResponse>.Error(
                    "Oops! Your subscription has expired. Renew today to pick up right where you left off!",
                    redirectToPackages: true,
                    response);
            }

            logMessages.Add("✓✓✓ LOGIN SUCCESSFUL ✓✓✓");
            WriteLog(logMessages, "login-success");

            return BaseResponse<LoginModelResponse>.Success("Logged in successfully", response);
        }

        private void WriteLog(List<string> messages, string status)
        {
            try
            {
                if (!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("Logs");
                }

                var fileName = $"Logs\\login-{status}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";
                File.WriteAllText(fileName, string.Join(Environment.NewLine, messages));
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}

