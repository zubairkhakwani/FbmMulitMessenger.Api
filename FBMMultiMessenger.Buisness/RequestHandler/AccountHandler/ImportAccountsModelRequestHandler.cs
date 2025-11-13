using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class ImportAccountsModelRequestHandler : IRequestHandler<ImportAccountsModelRequest, BaseResponse<UpsertAccountModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IEmailService _emailService;

        public ImportAccountsModelRequestHandler(ApplicationDbContext dbContext, IHubContext<ChatHub> hubContext, CurrentUserService currentUserService, IEmailService emailService)
        {
            this._dbContext=dbContext;
            this._hubContext=hubContext;
            this._currentUserService=currentUserService;
            this._emailService=emailService;
        }
        public async Task<BaseResponse<UpsertAccountModelResponse>> Handle(ImportAccountsModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra Safety Check, if user has came here he would be logged in hence the current user will never be null.
            if (currentUser == null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Invalid request, Please login again to continue.");
            }

            var currentUserId = currentUser.Id;

            var user = await _dbContext.Users
                                        .Include(a => a.Accounts)
                                        .Include(p => p.PasswordResetTokens)
                                        .Include(s => s.Subscriptions)
                                        .FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken);
            if (user is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("We couldn’t find your account. Please create an account to continue.");
            }

            if (!user.IsEmailVerified)
            {
                var isPreviousOtpValid = OtpManager.HasValidOtp(user, isEmailVerificationCode: true);

                if (isPreviousOtpValid)
                {
                    return BaseResponse<UpsertAccountModelResponse>.Success("A verification code has already been sent to your email. Please check your inbox.", result: new UpsertAccountModelResponse() { IsEmailVerified = false, EmailSendTo = user.Email });
                }

                var otp = OtpManager.GenerateOTP();

                var newPasswordResetToken = new PasswordResetToken()
                {
                    Email = user.Email,
                    Otp = otp,
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddMinutes(OtpManager.OtpExpiryDuration),
                    UserId = user.Id,
                    IsEmailVerification = true
                };

                await _dbContext.PasswordResetTokens.AddAsync(newPasswordResetToken, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                await _emailService.SendEmailVerificationEmailAsync(user.Email, otp, user.Name);
                return BaseResponse<UpsertAccountModelResponse>.Error("Please verify your email to continue.", result: new UpsertAccountModelResponse() { IsEmailVerified = false, EmailSendTo = user.Email });
            }


            var sanitizedAccounts = GetSanitizedAccounts(user.Accounts, request);

            var today = DateTime.UtcNow;
            var userSubscriptions = user.Subscriptions;

            var activeSubscription = userSubscriptions
                                          .Where(x => x.UserId == currentUserId
                                             &&
                                             x.StartedAt <= today
                                             &&
                                             x.ExpiredAt > today)
                                          .OrderByDescending(x => x.StartedAt)
                                          .FirstOrDefault();

            if (activeSubscription is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Oh Snap, It looks like you don’t have a subscription yet. Please subscribe to continue.", redirectToPackages: true);
            }

            var maxLimit = activeSubscription.MaxLimit;
            var limitUsed = activeSubscription.LimitUsed;
            var limitLeft = maxLimit - limitUsed;

            if (limitUsed >= maxLimit)
            {
                // response.IsLimitExceeded = true;
                return BaseResponse<UpsertAccountModelResponse>.Error("You’ve reached the maximum limit of your subscription plan. Please upgrade your plan.");
            }

            var expiryDate = activeSubscription.ExpiredAt;

            if (today >= expiryDate)
            {
                //response.IsSubscriptionExpired = true;
                return BaseResponse<UpsertAccountModelResponse>.Error("Your subscription has expired. Please renew to continue using this feature.");
            }

            sanitizedAccounts = sanitizedAccounts.Take(limitLeft).ToList();

            var newAccounts = sanitizedAccounts.Select(x => new Account()
            {
                Name = x.Name,
                FbAccountId = x.FbAccountId,
                Cookie = x.Cookie,
                UserId  = currentUserId,
                CreatedAt = DateTime.UtcNow,

            }).ToList();

            activeSubscription.LimitUsed+= sanitizedAccounts.Count;

            await _dbContext.Accounts.AddRangeAsync(newAccounts);
            await _dbContext.SaveChangesAsync();

            //Inform our server app to open browsers .
            var newAccountsHttpResponse = newAccounts.Select(x => new AccountDTO()
            {
                Id = x.Id,
                Name = x.Name,
                Cookie = x.Cookie,
                CreatedAt = x.CreatedAt,
            }).ToList();

            await _hubContext.Clients.Group($"Console_{currentUserId}")
                 .SendAsync("HandleUpsertAccount", newAccountsHttpResponse, cancellationToken);


            return BaseResponse<UpsertAccountModelResponse>.Success("Accounts added successfully", new());
        }

        public List<ImportAccounts> GetSanitizedAccounts(List<Account> userAccounts, ImportAccountsModelRequest request)
        {
            var uniqueAccounts = request.Accounts.DistinctBy(a => a.Cookie).ToList();

            for (int i = 0; i < uniqueAccounts.Count; i++)
            {
                var account = uniqueAccounts[i];

                var (isValid, fbAccountId)  = FBCookieValidatior.Validate(account.Cookie);

                if (!isValid || fbAccountId == null)
                {
                    uniqueAccounts.RemoveAt(i);
                    continue;
                }

                account.FbAccountId = fbAccountId;
            }

            var sanitizedAccounts = uniqueAccounts
                                    .Where(x => !userAccounts
                                    .Any(s => x.FbAccountId == s.FbAccountId)).ToList();

            return sanitizedAccounts;
        }
    }
}
