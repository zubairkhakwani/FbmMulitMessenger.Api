using AutoMapper;
using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Models.SignalR.LocalServer;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Extensions;
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
        UpsertAccountModelResponse response = new UpsertAccountModelResponse();
        public async Task<BaseResponse<UpsertAccountModelResponse>> Handle(ImportAccountsModelRequest request, CancellationToken cancellationToken)
        {
            try
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

                if (userSubscriptions.Count==0)
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("Oh Snap, It looks like you don’t have a subscription yet. Please subscribe to continue.");
                }

                var activeSubscription = _userAccountService.GetActiveSubscription(userSubscriptions);

                if (activeSubscription is null || _userAccountService.IsSubscriptionExpired(activeSubscription))
                {
                    response.IsSubscriptionExpired = true;
                    return BaseResponse<UpsertAccountModelResponse>.Error("Oops! Your subscription has expired. Kindly renew your plan to continue adding accounts in bulk.", redirectToPackages: true, result: response);
                }

                var isLimitReached = _userAccountService.HasLimitExceeded(activeSubscription);
                var limitLeft = activeSubscription.MaxLimit - activeSubscription.LimitUsed;

                if (isLimitReached)
                {
                    response.IsLimitExceeded = true;
                    return BaseResponse<UpsertAccountModelResponse>.Error("You’ve reached the maximum limit of your subscription plan. Please upgrade your plan.", showSweetAlert: true, result: response);
                }

                if (!user.IsEmailVerified)
                {
                    var emailVerificationResponse = await _userAccountService.ProcessEmailVerificationAsync(user, cancellationToken);

                    return mapper.Map<BaseResponse<UpsertAccountModelResponse>>(emailVerificationResponse);
                }

                var sanitizedAccounts = GetSanitizedAccounts(user.Accounts, user.Proxies, isProxyRequired: activeSubscription.CanRunOnOurServer, request);

                if (sanitizedAccounts.Count > 0)
                {
                    sanitizedAccounts = sanitizedAccounts.Take(limitLeft).ToList();

                    var newAccounts = sanitizedAccounts.Select(x => new Account()
                    {
                        Name = x.Name,
                        FbAccountId = x.FbAccountId,
                        Cookie = x.Cookie,
                        UserId  = currentUserId,
                        ConnectionStatus = AccountConnectionStatus.Offline,
                        AuthStatus = AccountAuthStatus.Idle,
                        Reason = AccountReason.NotAssignedToAnyLocalServer,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                    }).ToList();

                    await _dbContext.Accounts.AddRangeAsync(newAccounts, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    var eligibleServers = await _subscriptionServerProviderService.GetEligibleServersAsync(activeSubscription);
                    var powerfullEligibleServers = _localServerService.GetPowerfulServers(eligibleServers);

                    var serverAccountAssignments = new Dictionary<string, List<LocalServerAccountDTO>>();

                    if (powerfullEligibleServers is not null && powerfullEligibleServers.Count!=0)
                    {
                        foreach (var newAccount in newAccounts)
                        {
                            var leastLoadedServer = _localServerService.GetLeastLoadedServer(powerfullEligibleServers);

                            if (leastLoadedServer?.ActiveBrowserCount < leastLoadedServer?.MaxBrowserCapacity)
                            {
                                newAccount.LocalServerId = leastLoadedServer.Id;
                                newAccount.ConnectionStatus = AccountConnectionStatus.Starting;
                                newAccount.AuthStatus = AccountAuthStatus.Idle;
                                newAccount.Reason = AccountReason.AssigningToLocalServer;

                                leastLoadedServer.ActiveBrowserCount++;

                                var accountDTO = new LocalServerAccountDTO
                                {
                                    Id = newAccount.Id,
                                    Name = newAccount.Name,
                                    Cookie = newAccount.Cookie,
                                    CreatedAt = newAccount.CreatedAt,
                                };

                                if (!serverAccountAssignments.ContainsKey(leastLoadedServer.UniqueId))
                                {
                                    serverAccountAssignments[leastLoadedServer.UniqueId] = new List<LocalServerAccountDTO>();
                                }

                                serverAccountAssignments[leastLoadedServer.UniqueId].Add(accountDTO);
                            }
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
                }

                var showSweetAlert = response.SkippedAccounts.Count > 0;

                var responseMessage = response.SkippedAccounts.Count > 0
                    ? $"Successfully imported {response.SuccessfullyValidated} out of {response.TotalProcessed} accounts"
                    : "Accounts imported successfully.";

                return BaseResponse<UpsertAccountModelResponse>.Success(responseMessage, showSweetAlert: showSweetAlert,
                    result: new UpsertAccountModelResponse()
                    {
                        SkippedAccounts = response.SkippedAccounts,
                        TotalProcessed = response.TotalProcessed,
                        SuccessfullyValidated = response.SuccessfullyValidated,
                        IsEmailVerified = true,
                    });
            }
            catch (Exception)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Something went wrong, please try later");
            }
        }

        public List<ImportAccounts> GetSanitizedAccounts(
                                                           List<Account> existingUserAccounts,
                                                           List<Data.Database.DbModels.Proxy> userProxies,
                                                           bool isProxyRequired,
                                                           ImportAccountsModelRequest request)
        {
            var incomingAccounts = request.Accounts;
            var uniqueAccounts = new List<ImportAccounts>();
            var seenCookies = new HashSet<string>();

            foreach (var account in request.Accounts)
            {
                if (seenCookies.Contains(account.Cookie))
                {
                    response.SkippedAccounts.Add(new SkippedAccountModelResponse
                    {
                        Name = account.Name,
                        ProxyId = account.ProxyId,
                        Reason = AccountSkipReason.DuplicateCookie.GetInfo().Name
                    });

                    continue;
                }

                seenCookies.Add(account.Cookie);
                uniqueAccounts.Add(account);
            }

            var validatedAccounts = new List<ImportAccounts>();

            foreach (var account in uniqueAccounts)
            {
                // Validate proxy if provided
                if (!string.IsNullOrWhiteSpace(account.ProxyId))
                {
                    if (!int.TryParse(account.ProxyId, out int proxyId))
                    {
                        response.SkippedAccounts.Add(new SkippedAccountModelResponse
                        {
                            Name = account.Name,
                            ProxyId = account.ProxyId,
                            Reason = AccountSkipReason.InvalidProxyId.GetInfo().Name
                        });

                        continue;
                    }

                    bool isUserOwnedProxy = userProxies.Any(p => p.Id == proxyId);
                    if (!isUserOwnedProxy)
                    {
                        response.SkippedAccounts.Add(new SkippedAccountModelResponse
                        {
                            Name = account.Name,
                            ProxyId = account.ProxyId,
                            Reason = AccountSkipReason.UnauthorizedProxy.GetInfo().Name
                        });
                        continue;
                    }
                }
                else if (isProxyRequired)
                {
                    response.SkippedAccounts.Add(new SkippedAccountModelResponse
                    {
                        Name = account.Name,
                        ProxyId = account.ProxyId,
                        Reason = AccountSkipReason.MissingRequiredProxy.GetInfo().Name
                    });
                    continue;
                }

                var (isValidCookie, fbAccountId) = FBCookieValidatior.Validate(account.Cookie);

                if (!isValidCookie || fbAccountId == null)
                {
                    response.SkippedAccounts.Add(new SkippedAccountModelResponse
                    {
                        Name = account.Name,
                        ProxyId = account.ProxyId,
                        Reason = AccountSkipReason.InvalidCookie.GetInfo().Name
                    });
                    continue;
                }

                account.FbAccountId = fbAccountId;

                bool accountAlreadyExists = existingUserAccounts.Any(existing => existing.FbAccountId == fbAccountId && existing.IsActive);

                if (accountAlreadyExists)
                {
                    response.SkippedAccounts.Add(new SkippedAccountModelResponse
                    {
                        Name = account.Name,
                        ProxyId = account.ProxyId,
                        Reason = AccountSkipReason.AccountAlreadyExists.GetInfo().Name
                    });

                    continue;
                }
                validatedAccounts.Add(account);
            }

            response.SuccessfullyValidated = validatedAccounts.Count;
            response.TotalProcessed = incomingAccounts.Count;

            return validatedAccounts;
        }
    }
}
