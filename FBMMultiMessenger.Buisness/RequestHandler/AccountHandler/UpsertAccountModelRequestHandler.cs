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
    public class UpsertAccountModelRequestHandler(ApplicationDbContext _dbContext, CurrentUserService _currentUserService, IUserAccountService _userAccountService, ISubscriptionServerProviderService _subscriptionServerProviderService, ILocalServerService _localServerService, IHubContext<ChatHub> _hubContext, IMapper _mapper) : IRequestHandler<UpsertAccountModelRequest, BaseResponse<UpsertAccountModelResponse>>
    {
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
            try
            {
                var user = await _dbContext.Users
                                       .Include(p => p.Proxies)
                                       .Include(a => a.Accounts)
                                       .Include(p => p.VerificationTokens)
                                       .Include(s => s.Subscriptions)
                                       .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken: cancellationToken);

                if (user is null)
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("We couldn’t find your account. Please create an account to continue.");
                }

                var alreadyExistedAccount = user.Accounts.FirstOrDefault(x => x.FbAccountId == fbAccountId);

                if (alreadyExistedAccount != null && alreadyExistedAccount.IsActive)
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("This account is already being used, please provide another valid facebook cookie.");
                }

                var userSubscriptions = user.Subscriptions;
                var response = new UpsertAccountModelResponse();

                var activeSubscription = _userAccountService.GetActiveSubscription(userSubscriptions);

                if (activeSubscription is null)
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("Oh Snap, Looks like you don't have any subscription yet.", redirectToPackages: true);
                }

                if (_userAccountService.IsSubscriptionExpired(activeSubscription))
                {
                    response.IsSubscriptionExpired = true;
                    return BaseResponse<UpsertAccountModelResponse>.Error("Oops! Your subscription has expired. Kindly renew your plan to continue adding accounts.", redirectToPackages: true, response);
                }

                var isLimitReaced = _userAccountService.HasLimitExceeded(activeSubscription);

                if (isLimitReaced)
                {
                    response.IsLimitExceeded = true;
                    return BaseResponse<UpsertAccountModelResponse>.Error("You’ve reached the maximum limit of your subscription plan. Please upgrade your plan.", result: response);
                }

                if (activeSubscription.CanRunOnOurServer && string.IsNullOrWhiteSpace(request.ProxyId))
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("Please provide proxy when adding account");
                }

                int? proxyId = null;

                if (!string.IsNullOrWhiteSpace(request.ProxyId))
                {
                    var userProxies = user.Proxies;

                    proxyId = string.IsNullOrWhiteSpace(request.ProxyId) ? null : Convert.ToInt32(request.ProxyId);

                    var isValidUserProxy = userProxies.Any(p => p.Id == proxyId);

                    if (!isValidUserProxy)
                    {
                        return BaseResponse<UpsertAccountModelResponse>.Error("Proxy does not exist, please provide valid proxy.");
                    }
                }

                if (!user.IsEmailVerified)
                {
                    var emailVerificationResponse = await _userAccountService.ProcessEmailVerificationAsync(user, cancellationToken);

                    return _mapper.Map<BaseResponse<UpsertAccountModelResponse>>(emailVerificationResponse);
                }

                var elegibleServers = await _subscriptionServerProviderService.GetEligibleServersAsync(activeSubscription);
                var powerfullEligibleServer = _localServerService.GetPowerfulServers(elegibleServers);
                var assignedServer = _localServerService.GetLeastLoadedServer(powerfullEligibleServer);

                AccountDTO newAccountHttpResponse = null;
                if (alreadyExistedAccount == null)
                {
                    var newAccount = new Account()
                    {
                        UserId = request.UserId,
                        Cookie = request.Cookie,
                        Name = request.Name,
                        FbAccountId = fbAccountId,
                        ConnectionStatus = assignedServer is null ? AccountConnectionStatus.Offline : AccountConnectionStatus.Starting,
                        AuthStatus = AccountAuthStatus.Idle,
                        LocalServerId = assignedServer?.Id,
                        ProxyId = proxyId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _dbContext.Accounts.AddAsync(newAccount, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    newAccountHttpResponse = new AccountDTO()
                    {
                        Id = newAccount.Id,
                        Name = newAccount.Name,
                        Cookie = newAccount.Cookie,
                        CreatedAt = newAccount.CreatedAt
                    };
                }
                else
                {
                    alreadyExistedAccount.IsActive = true;
                    alreadyExistedAccount.Cookie = request.Cookie;
                    alreadyExistedAccount.Name = request.Name;
                    alreadyExistedAccount.ConnectionStatus = assignedServer is null ? AccountConnectionStatus.Offline : AccountConnectionStatus.Starting;
                    alreadyExistedAccount.AuthStatus = AccountAuthStatus.Idle;
                    alreadyExistedAccount.LocalServerId = assignedServer?.Id;
                    alreadyExistedAccount.ProxyId = proxyId;

                    _dbContext.Accounts.Update(alreadyExistedAccount);
                    await _dbContext.SaveChangesAsync();

                    newAccountHttpResponse = new AccountDTO()
                    {
                        Id = alreadyExistedAccount.Id,
                        Name = alreadyExistedAccount.Name,
                        Cookie = alreadyExistedAccount.Cookie,
                        CreatedAt = alreadyExistedAccount.CreatedAt
                    };
                }

                activeSubscription.LimitUsed++;

                if (assignedServer is not null)
                {
                    assignedServer.ActiveBrowserCount++;
                }


                if (assignedServer is not null)
                {
                    // Inform our local server to open a new browser instance.
                    await _hubContext.Clients.Group($"{assignedServer.UniqueId}")
                       .SendAsync("HandleAccountAdd", newAccountHttpResponse, cancellationToken);
                }

                return BaseResponse<UpsertAccountModelResponse>.Success("Account created successfully", response);
            }
            catch (Exception)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Something went wrong, pleas try later");
            }
        }

        private async Task<BaseResponse<UpsertAccountModelResponse>> UpdateRequestAsync(UpsertAccountModelRequest request, string fbAccountId, CancellationToken cancellationToken)
        {
            var account = await _dbContext.Accounts
                                          .Include(u => u.User)
                                          .ThenInclude(ls => ls.LocalServers)
                                          .Include(u => u.User)
                                          .ThenInclude(p => p.Proxies)
                                          .Include(u => u.User)
                                          .ThenInclude(s => s.Subscriptions)
                                          .FirstOrDefaultAsync(x => x.Id == request.AccountId
                                                               &&
                                                               x.UserId == request.UserId, cancellationToken);

            if (account is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Account does not exist.");
            }

            var user = account.User;

            var activeSubscription = _userAccountService.GetActiveSubscription(user.Subscriptions)
                                     ?? _userAccountService.GetLastActiveSubscription(user.Subscriptions);


            //Extra Safety Check as this point user must have an active subscription.
            if (activeSubscription is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Oh Snap, Looks like you don't have any subscription yet.", redirectToPackages: true);
            }

            if (activeSubscription.CanRunOnOurServer && string.IsNullOrWhiteSpace(request.ProxyId))
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Please provide proxy when editing account");
            }

            int? proxyId = null;

            if (!string.IsNullOrWhiteSpace(request.ProxyId))
            {
                var userProxies = user.Proxies;

                proxyId = string.IsNullOrWhiteSpace(request.ProxyId) ? null : Convert.ToInt32(request.ProxyId); ;

                var isValidUserProxy = userProxies.Any(p => p.Id == proxyId);

                if (!isValidUserProxy)
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("Proxy does not exist, please provide valid proxy.");
                }
            }

            if (!user.IsEmailVerified)
            {
                var emailVerificationResponse = await _userAccountService.ProcessEmailVerificationAsync(user, cancellationToken);

                return _mapper.Map<BaseResponse<UpsertAccountModelResponse>>(emailVerificationResponse);
            }

            var isCookieChanged = account.Cookie != request.Cookie;

            //on which localserver this account is currently running 
            var accountLocalServer = account.LocalServer;

            //If account is not assigned to any local server
            if (accountLocalServer is null)
            {
                var elegibleServers = await _subscriptionServerProviderService.GetEligibleServersAsync(activeSubscription);
                var powerfullEligibleServer = _localServerService.GetPowerfulServers(elegibleServers);
                var assignedServer = _localServerService.GetLeastLoadedServer(powerfullEligibleServer);

                if (assignedServer is not null)
                {
                    account.LocalServerId = assignedServer.Id;
                    assignedServer.ActiveBrowserCount++;

                    accountLocalServer = assignedServer;
                }
            }

            // Update account details
            account.Name = request.Name;
            account.Cookie = request.Cookie;
            account.FbAccountId = fbAccountId;
            account.ProxyId = proxyId;
            account.UpdatedAt = DateTime.UtcNow;

            if (isCookieChanged)
            {
                account.ConnectionStatus = AccountConnectionStatus.Starting;
                account.AuthStatus = AccountAuthStatus.Idle;
            }
            if (accountLocalServer is null)
            {
                account.ConnectionStatus = AccountConnectionStatus.Offline;
                account.AuthStatus = AccountAuthStatus.Idle;
            }

            _dbContext.Accounts.Update(account);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (accountLocalServer is not null)
            {
                //Inform user's local server.
                var newAccountHttpResponse = new AccountDTO()
                {
                    Id = account.Id,
                    Name = account.Name,
                    Cookie = account.Cookie,
                    CreatedAt = account.CreatedAt,
                    IsCookieChanged = isCookieChanged,
                };

                await _hubContext.Clients.Group($"{accountLocalServer.UniqueId}")
               .SendAsync("HandleAccountUpdate", newAccountHttpResponse, cancellationToken);
            }

            return BaseResponse<UpsertAccountModelResponse>.Success("Account updated successfully", new UpsertAccountModelResponse());
        }


        #region Helper Method
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

        #endregion
    }
}
