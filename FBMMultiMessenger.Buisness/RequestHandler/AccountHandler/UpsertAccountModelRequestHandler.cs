using Azure;
using Azure.Core;
using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Contracts.Account;
using FBMMultiMessenger.Contracts.Response;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System;
using System.Globalization;
using System.Security.Principal;
using System.Threading;

namespace FBMMultiMessenger.Buisness.RequestHandler.cs.AccountHandler
{
    public class UpsertAccountModelRequestHandler : IRequestHandler<UpsertAccountModelRequest, BaseResponse<UpsertAccountModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IHubContext<ChatHub> _hubContext;

        public UpsertAccountModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IHubContext<ChatHub> hubContext)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
            this._hubContext = hubContext;
        }
        public async Task<BaseResponse<UpsertAccountModelResponse>> Handle(UpsertAccountModelRequest request, CancellationToken cancellationToken)
        {
            (bool isValid, int? currentUserId, string? fbAccountId, string errorMessage) = ValidateRequest(request.Cookie, cancellationToken);

            if (!isValid)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error(errorMessage);
            }

            request.UserId = currentUserId!.Value;

            if (request.AccountId is null)
            {
                return await AddRequestAsync(request, fbAccountId!, cancellationToken);
            }

            return await UpdateRequestAsync(request, fbAccountId!, cancellationToken);
        }

        private async Task<BaseResponse<UpsertAccountModelResponse>> AddRequestAsync(UpsertAccountModelRequest request, string fbAccountId, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                                       .Include(a => a.Accounts)
                                       .Include(s => s.Subscriptions)
                                       .FirstOrDefaultAsync(x => x.Id == request.UserId);
            if (user is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("We couldn’t find your account. Please create an account to continue.");
            }


            var userAccounts = user.Accounts;
            var isFbAccountAlreadyExist = userAccounts.Any(x => x.FbAccountId == fbAccountId);

            if (isFbAccountAlreadyExist)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("This account is already being used, please provide another valid facebook cookie.");
            }


            var today = DateTime.UtcNow;
            var userSubscriptions = user.Subscriptions;

            var activeSubscription = userSubscriptions
                                          .Where(x => x.UserId == request.UserId
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

            var response = new UpsertAccountModelResponse();

            if (limitUsed >= maxLimit)
            {
                response.IsLimitExceeded = true;
                return BaseResponse<UpsertAccountModelResponse>.Error("You’ve reached the maximum limit of your subscription plan. Please upgrade your plan.", result: response);
            }

            var expiryDate = activeSubscription.ExpiredAt;

            if (today >= expiryDate)
            {
                response.IsSubscriptionExpired = true;
                return BaseResponse<UpsertAccountModelResponse>.Error("Your subscription has expired. Please renew to continue using this feature.", redirectToPackages: true, response);
            }

            var newAccount = new Account()
            {
                UserId = request.UserId,
                Cookie = request.Cookie,
                Name = request.Name,
                FbAccountId = fbAccountId,
                CreatedAt = DateTime.UtcNow
            };

            activeSubscription.LimitUsed++;

            await _dbContext.Accounts.AddAsync(newAccount, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var newAccountHttpResponse = new ConsoleAccountDTO()
            {
                Id =  newAccount.Id,
                Name = newAccount.Name,
                Cookie = newAccount.Cookie,
            };

            //Inform our console app to close/re-open browser accordingly.
            var consoleUser = $"Console_{request.UserId.ToString()}";
            await _hubContext.Clients.Group(consoleUser)
               .SendAsync("HandleUpsertAccount", newAccountHttpResponse, cancellationToken);

            return BaseResponse<UpsertAccountModelResponse>.Success("Account created successfully", response);
        }

        private async Task<BaseResponse<UpsertAccountModelResponse>> UpdateRequestAsync(UpsertAccountModelRequest request, string fbAccountId, CancellationToken cancellationToken)
        {
            var account = await _dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == request.AccountId && x.UserId == request.UserId);

            if (account is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Account does not exist.");
            }

            var shouldHandleBrowser = account.Cookie != request.Cookie;

            account.Name = request.Name;
            account.Cookie = request.Cookie;
            account.FbAccountId = fbAccountId;
            account.UpdatedAt = DateTime.Now;

            _dbContext.Accounts.Update(account);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = new UpsertAccountModelResponse();

            if (shouldHandleBrowser)
            {
                var newAccountHttpResponse = new ConsoleAccountDTO()
                {
                    Id =  account.Id,
                    Name = account.Name,
                    Cookie = account.Cookie,
                    IsUpdateRequest = true
                };

                //Inform our console app to close/re-open browser accordingly.
                var consoleUser = $"Console_{request.UserId.ToString()}";
                await _hubContext.Clients.Group(consoleUser)
                   .SendAsync("HandleUpsertAccount", newAccountHttpResponse, cancellationToken);
            }

            return BaseResponse<UpsertAccountModelResponse>.Success("Account updated successfully", response);
        }

        private (bool isValid, string? userId) ValidateCookie(string cookieString)
        {
            try
            {
                // Parse cookies into dictionary
                var cookies = cookieString
                    .Split(';')
                    .Select(x => x.Trim().Split('=', 2))
                    .Where(x => x.Length == 2)
                    .ToDictionary(x => x[0], x => x[1]);


                if (!cookies.ContainsKey("c_user") || !cookies.ContainsKey("xs"))
                    return (false, null);

                return (true, cookies["c_user"]);
            }
            catch
            {
                return (false, null);
            }
        }

        private (bool isValid, int? currentUserId, string? fbAccountIda, string errorMessage) ValidateRequest(string cookie, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra Safety Check, if user has came here he would be logged in hence the current user will never be null.
            if (currentUser is null)
            {
                return (false, null, null, "Invalid request, Please login again to continue.");
            }

            (bool IsValidCookie, string? fbAccountId) =  ValidateCookie(cookie);

            if (!IsValidCookie || string.IsNullOrWhiteSpace(fbAccountId))
            {

                return (false, null, null, "The cookie you provided is not valid. Please provide a valid Facebook cookie.");
            }

            return (true, currentUser.Id, fbAccountId, "Validation Successful");
        }
    }
}
