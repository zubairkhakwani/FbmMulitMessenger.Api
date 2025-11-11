using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    public class UpsertAccountModelRequestHandler : IRequestHandler<UpsertAccountModelRequest, BaseResponse<UpsertAccountModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IHubContext<ChatHub> _hubContext;

        public UpsertAccountModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IHubContext<ChatHub> hubContext)
        {
            _dbContext=dbContext;
            _currentUserService=currentUserService;
            _hubContext = hubContext;
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



            //Inform our server to open browser .
            var newAccountHttpResponse = new AccountDTO()
            {
                Id =  newAccount.Id,
                Name = newAccount.Name,
                Cookie = newAccount.Cookie,
                CreatedAt = newAccount.CreatedAt
            };

            await _hubContext.Clients.Group($"Console_{request.UserId}")
               .SendAsync("HandleUpsertAccount", new List<AccountDTO>() { newAccountHttpResponse }, cancellationToken);

            return BaseResponse<UpsertAccountModelResponse>.Success("Account created successfully", response);
        }

        private async Task<BaseResponse<UpsertAccountModelResponse>> UpdateRequestAsync(UpsertAccountModelRequest request, string fbAccountId, CancellationToken cancellationToken)
        {
            var account = await _dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == request.AccountId && x.UserId == request.UserId);

            if (account is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Account does not exist.");
            }

            var newCookie = request.Cookie.Trim();
            var previousCookie = account.Cookie.Trim();

            var newAccountName = request.Name.Trim();
            var previousAccountName = account.Name.Trim();

            var shouldUpdate = newCookie != previousCookie || newAccountName != previousAccountName;

            if (shouldUpdate)
            {
                account.Name = newAccountName;
                account.Cookie = newCookie;
                account.FbAccountId = fbAccountId;
                account.UpdatedAt = DateTime.UtcNow;

                _dbContext.Accounts.Update(account);
                await _dbContext.SaveChangesAsync(cancellationToken);

                //Inform our server to close/re-open browser accordingly.
                var newAccountHttpResponse = new AccountDTO()
                {
                    Id =  account.Id,
                    Name = account.Name,
                    Cookie = account.Cookie,
                    CreatedAt = account.CreatedAt,
                    IsCookieChanged = newCookie != previousCookie,
                };

                await _hubContext.Clients.Group($"Console_{request.UserId.ToString()}")
                   .SendAsync("HandleUpsertAccount", new List<AccountDTO>() { newAccountHttpResponse }, cancellationToken);
            }

            return BaseResponse<UpsertAccountModelResponse>.Success("Account updated successfully", new UpsertAccountModelResponse());
        }



        private (bool isValid, int? currentUserId, string? fbAccountIda, string errorMessage) ValidateRequest(string cookie, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra Safety Check, if user has came here he would be logged in hence the current user will never be null.
            if (currentUser is null)
            {
                return (false, null, null, "Invalid request, Please login again to continue.");
            }

            (bool IsValidCookie, string? fbAccountId) = FBCookieValidatior.Validate(cookie);

            if (!IsValidCookie || string.IsNullOrWhiteSpace(fbAccountId))
            {

                return (false, null, null, "The cookie you provided is not valid. Please provide a valid facebook cookie.");
            }

            return (true, currentUser.Id, fbAccountId, "Validation Successful");
        }
    }
}
