using AutoMapper;
using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Models.SignalR.LocalServer;
using FBMMultiMessenger.Buisness.Request.Account;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AccountHandler
{
    public class UpsertAccountModelRequestHandler(ApplicationDbContext _dbContext, CurrentUserService _currentUserService, IUserAccountService _userAccountService, ISubscriptionServerProviderService _subscriptionServerProviderService, ILocalServerService _localServerService, ISignalRService _signalRService, IMapper _mapper) : IRequestHandler<UpsertAccountModelRequest, BaseResponse<UpsertAccountModelResponse>>
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
                    return BaseResponse<UpsertAccountModelResponse>.Error("Oops! Your subscription has expired. Kindly renew your plan to continue adding accounts.", redirectToPackages: true, result: response);
                }

                var isLimitReaced = _userAccountService.HasLimitExceeded(activeSubscription);
                var incommingProxyId = request.ProxyId;

                if (isLimitReaced)
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("You’ve reached the maximum limit of your subscription plan. Please upgrade your plan.", showSweetAlert: true, result: response);
                }

                if (activeSubscription.CanRunOnOurServer && string.IsNullOrWhiteSpace(incommingProxyId))
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("Please provide proxy when adding account");
                }

                Data.Database.DbModels.Proxy? selectedProxy = null;

                if (!string.IsNullOrWhiteSpace(request.ProxyId))
                {
                    var userProxies = user.Proxies;

                    var proxy = userProxies.FirstOrDefault(p => p.Id == Convert.ToInt32(incommingProxyId));

                    if (proxy is null)
                    {
                        return BaseResponse<UpsertAccountModelResponse>.Error("Proxy does not exist, please provide valid proxy.");
                    }

                    selectedProxy = proxy;
                }

                if (!user.IsEmailVerified)
                {
                    var emailVerificationResponse = await _userAccountService.ProcessEmailVerificationAsync(user, cancellationToken);

                    return _mapper.Map<BaseResponse<UpsertAccountModelResponse>>(emailVerificationResponse);
                }

                var elegibleServers = await _subscriptionServerProviderService.GetEligibleServersAsync(activeSubscription);
                var powerfullEligibleServer = _localServerService.GetPowerfulServers(elegibleServers);
                var assignedServer = _localServerService.GetLeastLoadedServer(powerfullEligibleServer);

                var newAccount = new Account()
                {
                    UserId = request.UserId,
                    Cookie = request.Cookie,
                    Name = request.Name,
                    FbAccountId = fbAccountId,
                    ConnectionStatus = assignedServer is null ? AccountConnectionStatus.Offline : AccountConnectionStatus.Starting,
                    AuthStatus = AccountAuthStatus.Idle,
                    Reason = assignedServer is null ? AccountReason.NotAssignedToAnyLocalServer : AccountReason.AssigningToLocalServer,
                    LocalServerId = assignedServer?.Id,
                    ProxyId = selectedProxy?.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                if (assignedServer is not null)
                {
                    assignedServer.ActiveBrowserCount++;
                }
                activeSubscription.LimitUsed++;

                await _dbContext.Accounts.AddAsync(newAccount, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                var localServerAccountDTO = new LocalServerAccountDTO()
                {
                    Id = newAccount.Id,
                    Name = newAccount.Name,
                    Cookie = newAccount.Cookie,
                    CreatedAt = newAccount.CreatedAt,
                    Proxy = selectedProxy is null ? null : new LocalServerProxyDTO()
                    {
                        Id = selectedProxy.Id,
                        Ip_Port = selectedProxy.Ip_Port,
                        Name = selectedProxy.Name,
                        Password = selectedProxy.Password
                    }
                };

                if (assignedServer is not null)
                {
                    // Inform our local server to open a new browser instance.
                    await _signalRService.NotifyLocalServerAccountAdded(localServerAccountDTO, assignedServer.UniqueId, cancellationToken);
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

            var incommingProxyId = request.ProxyId;

            var activeSubscription = _userAccountService.GetActiveSubscription(user.Subscriptions)
                                     ?? _userAccountService.GetLastActiveSubscription(user.Subscriptions);


            //Extra Safety Check as this point user must have an active subscription.
            if (activeSubscription is null)
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Oh Snap, Looks like you don't have any subscription yet.", redirectToPackages: true);
            }

            if (activeSubscription.CanRunOnOurServer && string.IsNullOrWhiteSpace(incommingProxyId))
            {
                return BaseResponse<UpsertAccountModelResponse>.Error("Please provide proxy when editing account");
            }

            Data.Database.DbModels.Proxy? selectedProxy = null;

            if (!string.IsNullOrWhiteSpace(incommingProxyId))
            {
                var userProxies = user.Proxies;

                var proxy = userProxies.FirstOrDefault(p => p.Id == Convert.ToInt32(incommingProxyId));

                if (proxy is not null)
                {
                    return BaseResponse<UpsertAccountModelResponse>.Error("Proxy does not exist, please provide valid proxy.");
                }
                selectedProxy = proxy;
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
            account.ProxyId = selectedProxy?.Id;
            account.UpdatedAt = DateTime.UtcNow;

            if (isCookieChanged)
            {
                account.ConnectionStatus = AccountConnectionStatus.Starting;
                account.AuthStatus = AccountAuthStatus.Idle;
                account.Reason = AccountReason.AssigningToLocalServer;
            }

            if (accountLocalServer is null)
            {
                account.ConnectionStatus = AccountConnectionStatus.Offline;
                account.AuthStatus = AccountAuthStatus.Idle;
                account.Reason = AccountReason.NotAssignedToAnyLocalServer;
            }

            _dbContext.Accounts.Update(account);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (accountLocalServer is not null)
            {
                //Inform user's local server.
                var localServerAccountDTO = new LocalServerAccountDTO()
                {
                    Id = account.Id,
                    Name = account.Name,
                    Cookie = account.Cookie,
                    CreatedAt = account.CreatedAt,
                    IsCookieChanged = isCookieChanged,
                    Proxy = selectedProxy is null ? null : new LocalServerProxyDTO()
                    {
                        Id  = selectedProxy.Id,
                        Ip_Port = selectedProxy.Ip_Port,
                        Name = selectedProxy.Name,
                        Password = selectedProxy.Password
                    }
                };

                await _signalRService.NotifyLocalServerAccountUpdated(localServerAccountDTO, accountLocalServer.UniqueId, cancellationToken);
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
