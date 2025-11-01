﻿using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class ImportAccountsModelRequestHandler : IRequestHandler<ImportAccountsModelRequest, BaseResponse<object>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly CurrentUserService _currentUserService;

        public ImportAccountsModelRequestHandler(ApplicationDbContext dbContext, IHubContext<ChatHub> hubContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._hubContext=hubContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<object>> Handle(ImportAccountsModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra Safety Check, if user has came here he would be logged in hence the current user will never be null.
            if (currentUser == null)
            {
                return BaseResponse<object>.Error("Invalid request, Please login again to continue.");
            }

            var currentUserId = currentUser.Id;

            var user = await _dbContext.Users
                                        .Include(a => a.Accounts)
                                        .Include(s => s.Subscriptions)
                                        .FirstOrDefaultAsync(x => x.Id == currentUserId);
            if (user is null)
            {
                return BaseResponse<object>.Error("We couldn’t find your account. Please create an account to continue.");
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
                return BaseResponse<object>.Error("Oh Snap, It looks like you don’t have a subscription yet. Please subscribe to continue.", redirectToPackages: true);
            }

            var maxLimit = activeSubscription.MaxLimit;
            var limitUsed = activeSubscription.LimitUsed;
            var limitLeft = maxLimit - limitUsed;

            if (limitUsed >= maxLimit)
            {
                // response.IsLimitExceeded = true;
                return BaseResponse<object>.Error("You’ve reached the maximum limit of your subscription plan. Please upgrade your plan.");
            }

            var expiryDate = activeSubscription.ExpiredAt;

            if (today >= expiryDate)
            {
                //response.IsSubscriptionExpired = true;
                return BaseResponse<object>.Error("Your subscription has expired. Please renew to continue using this feature.");
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


            return BaseResponse<object>.Success("Accounts added successfully", new());
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
