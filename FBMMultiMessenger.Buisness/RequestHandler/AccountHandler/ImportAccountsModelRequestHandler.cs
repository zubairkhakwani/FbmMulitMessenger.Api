using AutoMapper;
using FBMMultiMessenger.Buisness.DTO;
using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    internal class ImportAccountsModelRequestHandler : IRequestHandler<ImportAccountsModelRequest, BaseResponse<UpsertAccountModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IUserAccountService _userAccountService;
        private readonly IMapper _mapper;
        private readonly CurrentUserService _currentUserService;

        public ImportAccountsModelRequestHandler(ApplicationDbContext dbContext, IHubContext<ChatHub> hubContext, IUserAccountService userAccountService, IMapper mapper, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._hubContext=hubContext;
            this._userAccountService=userAccountService;
            this._mapper=mapper;
            this._currentUserService=currentUserService;
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
                                       .Include(ls => ls.LocalServers)
                                       .Include(a => a.Accounts)
                                       .Include(p => p.VerificationTokens)
                                       .Include(s => s.Subscriptions)
                                       .FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken);
            if (user is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("We couldn’t find your account. Please create an account to continue.");
            }

            var userSubscriptions = user.Subscriptions;

            if (!userSubscriptions.Any())
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Oh Snap, It looks like you don’t have a subscription yet. Please subscribe to continue.");
            }

            var activeSubscription = _userAccountService.GetActiveSubscription(userSubscriptions);

            var response = new UpsertAccountModelResponse();

            if (activeSubscription is null || _userAccountService.IsSubscriptionExpired(activeSubscription))
            {
                response.IsSubscriptionExpired = true;
                return BaseResponse<UpsertAccountModelResponse>.Error("Oops! Your subscription has expired. Kindly renew your plan to continue adding accounts in bulk.", redirectToPackages: true, response);
            }

            var isLimitReaced = _userAccountService.HasLimitExceeded(activeSubscription);
            var limitLeft = activeSubscription.MaxLimit - activeSubscription.LimitUsed;

            if (isLimitReaced)
            {
                response.IsLimitExceeded = true;
                return BaseResponse<UpsertAccountModelResponse>.Error("You’ve reached the maximum limit of your subscription plan. Please upgrade your plan.", result: response);
            }

            if (!user.IsEmailVerified)
            {
                var emailVerificationResponse = await _userAccountService.ProcessEmailVerificationAsync(user, cancellationToken);

                return _mapper.Map<BaseResponse<UpsertAccountModelResponse>>(emailVerificationResponse);
            }

            var sanitizedAccounts = GetSanitizedAccounts(user.Accounts, request);
            sanitizedAccounts = sanitizedAccounts.Take(limitLeft).ToList();

            var newAccounts = sanitizedAccounts.Select(x => new Account()
            {
                Name = x.Name,
                FbAccountId = x.FbAccountId,
                Cookie = x.Cookie,
                UserId  = currentUserId,
                Status = AccountStatus.Inactive,
                CreatedAt = DateTime.UtcNow,

            }).ToList();

            activeSubscription.LimitUsed+= sanitizedAccounts.Count;

            await _dbContext.Accounts.AddRangeAsync(newAccounts, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);


            var userLocalServers = user.LocalServers;
            var powerFullServer = LocalServerHelper.GetAvailablePowerfulServer(userLocalServers);

            if (powerFullServer is not null)
            {
                var remainingSlots = powerFullServer.MaxBrowserCapacity - powerFullServer.MaxBrowserCapacity;

                //Inform our local server app to open browsers.
                var newAccountsHttpResponse = newAccounts.Select(x => new AccountDTO()
                {
                    Id = x.Id,
                    Name = x.Name,
                    Cookie = x.Cookie,
                    CreatedAt = x.CreatedAt,
                }).Take(remainingSlots).ToList();

                await _hubContext.Clients.Group($"{powerFullServer.UniqueId}")
                     .SendAsync("HandleImportAccounts", newAccountsHttpResponse, cancellationToken);
            }

            return BaseResponse<UpsertAccountModelResponse>.Success("Accounts added successfully", new UpsertAccountModelResponse() { IsEmailVerified = true });
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
