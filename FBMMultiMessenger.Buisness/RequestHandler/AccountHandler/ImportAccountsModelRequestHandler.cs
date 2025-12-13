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
    internal class ImportAccountsModelRequestHandler(ApplicationDbContext _dbContext, IHubContext<ChatHub> _hubContext, IUserAccountService _userAccountService, ISubscriptionServerProviderService _subscriptionServerProviderService, ILocalServerService _localServerService, IMapper mapper, CurrentUserService currentUserService) : IRequestHandler<ImportAccountsModelRequest, BaseResponse<UpsertAccountModelResponse>>
    {
        public async Task<BaseResponse<UpsertAccountModelResponse>> Handle(ImportAccountsModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = currentUserService.GetCurrentUser();

            //Extra Safety Check, if user has came here he would be logged in hence the current user will never be null.
            if (currentUser == null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Invalid request, Please login again to continue.");
            }

            var currentUserId = currentUser.Id;

            var user = await _dbContext.Users
                                       .Include(p => p.Proxies)
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

            var isProxyNotProvided = request.Accounts.Any(x => x.ProxyId == null);

            if (activeSubscription.CanRunOnOurServer && isProxyNotProvided)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Please provide proxy for all the accounts", result: response);
            }

            if (!user.IsEmailVerified)
            {
                var emailVerificationResponse = await _userAccountService.ProcessEmailVerificationAsync(user, cancellationToken);

                return mapper.Map<BaseResponse<UpsertAccountModelResponse>>(emailVerificationResponse);
            }

            var sanitizedAccounts = GetSanitizedAccounts(user.Accounts, user.Proxies, isProxyRequired: activeSubscription.CanRunOnOurServer, request);

            if (sanitizedAccounts.Count == 0)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Accounts data is invalid. Please check cookies and proxies and try again.", result: response);

            }

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

            await _dbContext.Accounts.AddRangeAsync(newAccounts, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var eligibleServers = await _subscriptionServerProviderService.GetEligibleServersAsync(activeSubscription);
            var powerfullEligibleServers = _localServerService.GetPowerfulServers(eligibleServers);

            var serverAccountAssignments = new Dictionary<string, List<AccountDTO>>();

            foreach (var newAccount in newAccounts)
            {
                var leastLoadedServer = _localServerService.GetLeastLoadedServer(powerfullEligibleServers);

                if (leastLoadedServer?.ActiveBrowserCount < leastLoadedServer?.MaxBrowserCapacity)
                {
                    newAccount.LocalServerId = leastLoadedServer.Id;
                    newAccount.Status = AccountStatus.InProgress;
                    leastLoadedServer.ActiveBrowserCount++;

                    var accountDTO = new AccountDTO
                    {
                        Id = newAccount.Id,
                        Name = newAccount.Name,
                        Cookie = newAccount.Cookie,
                        CreatedAt = newAccount.CreatedAt,
                    };

                    if (!serverAccountAssignments.ContainsKey(leastLoadedServer.UniqueId))
                    {
                        serverAccountAssignments[leastLoadedServer.UniqueId] = new List<AccountDTO>();
                    }

                    serverAccountAssignments[leastLoadedServer.UniqueId].Add(accountDTO);
                }
            }

            activeSubscription.LimitUsed+= sanitizedAccounts.Count;

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Send assignments to servers via SignalR
            foreach (var (uniqueId, accounts) in serverAccountAssignments)
            {
                try
                {
                    //here unique id is the identifier that tells signalR Connection
                    await _hubContext.Clients.Group($"{uniqueId}")
                        .SendAsync("HandleImportAccounts", accounts, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to notify server {uniqueId}: {ex.Message}");
                }
            }

            return BaseResponse<UpsertAccountModelResponse>.Success("Accounts added successfully", new UpsertAccountModelResponse() { IsEmailVerified = true });
        }

        public List<ImportAccounts> GetSanitizedAccounts(
                                                           List<Account> existingUserAccounts,
                                                           List<Data.Database.DbModels.Proxy> userProxies,
                                                           bool isProxyRequired,
                                                           ImportAccountsModelRequest request)
        {
            var uniqueAccounts = request.Accounts
                .DistinctBy(a => a.Cookie)
                .ToList();

            var validatedAccounts = new List<ImportAccounts>();

            foreach (var account in uniqueAccounts)
            {
                // Validate proxy if provided
                if (!string.IsNullOrWhiteSpace(account.ProxyId))
                {
                    if (!int.TryParse(account.ProxyId, out int proxyId))
                    {
                        continue;
                    }

                    bool isUserOwnedProxy = userProxies.Any(p => p.Id == proxyId);
                    if (!isUserOwnedProxy)
                    {
                        continue;
                    }
                }
                else if (isProxyRequired)
                {
                    continue;
                }

                var (isValidCookie, fbAccountId) = FBCookieValidatior.Validate(account.Cookie);

                if (!isValidCookie || fbAccountId == null)
                {
                    continue;
                }

                account.FbAccountId = fbAccountId;

                bool accountAlreadyExists = existingUserAccounts.Any(existing => existing.FbAccountId == fbAccountId);

                if (!accountAlreadyExists)
                {
                    validatedAccounts.Add(account);
                }
            }

            return validatedAccounts;
        }
    }
}
